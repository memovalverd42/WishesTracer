using Microsoft.Playwright;

namespace Infraestructure.Scraper.Core;

public class PlaywrightEngine : IAsyncDisposable
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private bool _isInitialized = false;

    // Configuración para parecer humano
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        _playwright = await Playwright.CreateAsync();

        // Lanzamos el navegador UNA sola vez. 
        // Headless = true para producción, false para ver qué hace mientras programas.
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true, // Ponlo en false ahora para que veas la magia
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
        Console.WriteLine("Motor Playwright Iniciado (Browser levantado).");
    }

    public async Task<string?> GetHtmlContentAsync(string url)
    {
        if (!_isInitialized) await InitializeAsync();

        // Creamos un contexto efímero (como una pestaña de incógnito aislada)
        // Esto es muy barato en memoria.
        await using var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = UserAgent,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            // Desactiva popups de descarga y notificaciones
            AcceptDownloads = false,
            // Ignora errores de HTTPS
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();

        // OPTIMIZACIÓN CRÍTICA: Bloquear imágenes, fuentes y CSS
        // Usamos FulfillAsync en lugar de AbortAsync para evitar errores de red
        await page.RouteAsync("**/*.{png,jpg,jpeg,svg,gif,woff,woff2,ttf,otf,eot,ico}", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/plain",
                Body = ""
            });
        });
        
        // Bloquear CSS con respuesta vacía
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

        try
        {
            // Timeout defensivo de 30 segundos
            await page.GotoAsync(url,
                new PageGotoOptions { Timeout = 30000, WaitUntil = WaitUntilState.DOMContentLoaded });

            // Aquí esperamos a que aparezca algo clave del precio (selector genérico)
            // O simplemente devolvemos el HTML para que la estrategia lo procese.
            // Para Amazon, esperamos un poco a que JS pinte precios.
            await page.WaitForTimeoutAsync(2000);

            return await page.ContentAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error navegando a {url}: {ex.Message}");
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}
