using Microsoft.Playwright;
using Serilog;

namespace WishesTracer.Infraestructure.Scraper.Core;

public class PlaywrightEngine : IAsyncDisposable
{
    private readonly ILogger _logger = Log.ForContext<PlaywrightEngine>();
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _isInitialized;

    private readonly SemaphoreSlim _initLock = new(1, 1);

    // Configuración para parecer humano
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            // Chequeo doble (Double-check locking pattern)
            if (_isInitialized) return;
            
            _logger.Information("Iniciando motor Playwright...");
            
            try
            {
                _playwright = await Playwright.CreateAsync();
                _logger.Debug("Playwright API creada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error crítico al crear instancia de Playwright. Verifique que los binarios estén instalados (pwsh bin/Debug/net8.0/playwright.ps1 install)");
                throw new InvalidOperationException("No se pudo inicializar Playwright. Ejecute: pwsh bin/Debug/net8.0/playwright.ps1 install", ex);
            }

            // Lanzamiento del navegador UNA sola vez.
            try
            {
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args =
                    [
                        "--disable-blink-features=AutomationControlled", // Evita detección de bot
                        "--disable-notifications", // Sin popups de notificaciones
                        "--disable-popup-blocking", // Paradójicamente ayuda a evitar popups de descarga
                        "--no-first-run", // Sin pantallas de bienvenida
                        "--no-default-browser-check", // Sin preguntas de navegador predeterminado
                        "--disable-extensions", // Sin extensiones
                        "--disable-infobars", // Sin barras de información
                        "--disable-dev-shm-usage", // Mejora rendimiento en Docker/Linux
                        "--disable-background-networking", // Menos tráfico de fondo
                        "--disable-features=TranslateUI", // Sin popups de traducción
                        "--disable-save-password-bubble" // Sin popups de guardar contraseña
                    ]
                });
                
                _isInitialized = true;
                _logger.Information("Motor Playwright iniciado exitosamente. Browser Chromium levantado");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error al lanzar el navegador Chromium. Puede que falten dependencias del sistema");
                throw new InvalidOperationException("No se pudo lanzar el navegador Chromium", ex);
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string?> GetHtmlContentAsync(string url)
    {
        await InitializeAsync();
        
        if (_browser == null)
        {
            _logger.Error("Browser no inicializado al intentar acceder a {Url}", url);
            throw new InvalidOperationException("Browser no inicializado");
        }

        _logger.Debug("Iniciando navegación a {Url}", url);

        IBrowserContext? context = null;
        IPage? page = null;

        try
        {
            // Creamos un contexto efímero (como una pestaña de incógnito aislada)
            context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = UserAgent,
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                AcceptDownloads = false,
                IgnoreHTTPSErrors = true
            });

            page = await context.NewPageAsync();
            _logger.Debug("Página y contexto creados para {Url}", url);

            // OPTIMIZACIÓN CRÍTICA: Bloquear recursos innecesarios
            await page.RouteAsync("**/*.{png,jpg,jpeg,svg,gif,woff,woff2,ttf,otf,eot,ico}", async route =>
            {
                await route.FulfillAsync(new RouteFulfillOptions
                {
                    Status = 200,
                    ContentType = "text/plain",
                    Body = ""
                });
            });

            await page.RouteAsync("**/*.css", async route =>
            {
                await route.FulfillAsync(new RouteFulfillOptions
                {
                    Status = 200,
                    ContentType = "text/css",
                    Body = ""
                });
            });

            // Bloquear analytics y tracking
            await page.RouteAsync("**/*{google-analytics,googletagmanager,facebook,doubleclick,analytics}*",
                async route => await route.AbortAsync());

            _logger.Debug("Rutas de bloqueo configuradas, navegando a {Url}...", url);

            try
            {
                // Timeout defensivo de 30 segundos
                var response = await page.GotoAsync(url, new PageGotoOptions
                {
                    Timeout = 30000,
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });

                if (response == null)
                {
                    _logger.Warning("No se recibió respuesta al navegar a {Url}", url);
                    return null;
                }

                if (!response.Ok)
                {
                    _logger.Warning("Respuesta HTTP no exitosa para {Url}: Status={Status}", url, response.Status);
                }
                else
                {
                    _logger.Debug("Navegación exitosa a {Url} - Status: {Status}", url, response.Status);
                }

                // Esperar a que el JavaScript pinte el contenido
                await page.WaitForTimeoutAsync(2000);

                var htmlContent = await page.ContentAsync();
                _logger.Information("HTML obtenido exitosamente de {Url} - Tamaño: {Size} caracteres", url, htmlContent.Length);

                return htmlContent;
            }
            catch (TimeoutException ex)
            {
                _logger.Error(ex, "Timeout al navegar a {Url}. La página tardó más de 30 segundos en cargar", url);
                return null;
            }
            catch (PlaywrightException pex) when (pex.Message.Contains("net::ERR_NAME_NOT_RESOLVED"))
            {
                _logger.Error("No se pudo resolver el nombre DNS para {Url}. Verifique la URL o conexión a Internet", url);
                return null;
            }
            catch (PlaywrightException pex) when (pex.Message.Contains("net::ERR_CONNECTION_REFUSED"))
            {
                _logger.Error("Conexión rechazada al intentar acceder a {Url}. El servidor puede estar caído", url);
                return null;
            }
            catch (PlaywrightException pex) when (pex.Message.Contains("net::ERR_CONNECTION_TIMED_OUT"))
            {
                _logger.Error("Timeout de conexión al intentar acceder a {Url}. Problemas de red o servidor lento", url);
                return null;
            }
            catch (PlaywrightException pex) when (pex.Message.Contains("net::ERR_CERT"))
            {
                _logger.Error(pex, "Error de certificado SSL al acceder a {Url}", url);
                return null;
            }
            catch (PlaywrightException pex) when (pex.Message.Contains("Target page, context or browser has been closed"))
            {
                _logger.Error("La página, contexto o navegador se cerró inesperadamente durante la navegación a {Url}", url);
                return null;
            }
            catch (PlaywrightException pex)
            {
                _logger.Error(pex, "Error de Playwright al navegar a {Url}: {Message}", url, pex.Message);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error inesperado al procesar {Url}", url);
            return null;
        }
        finally
        {
            // Limpieza de recursos
            try
            {
                if (page != null)
                {
                    await page.CloseAsync();
                    _logger.Debug("Página cerrada para {Url}", url);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error al cerrar página para {Url}", url);
            }

            try
            {
                if (context != null)
                {
                    await context.DisposeAsync();
                    _logger.Debug("Contexto liberado para {Url}", url);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error al liberar contexto para {Url}", url);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.Debug("Iniciando liberación de recursos de PlaywrightEngine");

        try
        {
            if (_browser != null)
            {
                await _browser.DisposeAsync();
                _logger.Information("Browser Chromium cerrado exitosamente");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al cerrar el navegador durante dispose");
        }

        try
        {
            _playwright?.Dispose();
            _logger.Information("Motor Playwright liberado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al liberar Playwright durante dispose");
        }

        _isInitialized = false;
        _logger.Debug("PlaywrightEngine completamente liberado");
    }
}
