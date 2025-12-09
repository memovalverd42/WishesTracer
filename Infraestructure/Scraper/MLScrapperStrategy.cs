using System.Globalization;
using HtmlAgilityPack;
using WishesTracer.Application.DTOs;

namespace Infraestructure.Scraper;

public class MercadoLibreStrategy : IScraperStrategy
{
    public bool CanHandle(string url) => url.Contains("mercadolibre", StringComparison.OrdinalIgnoreCase) ||
                                      url.EndsWith("mercadolibre.com.mx", StringComparison.OrdinalIgnoreCase) ||
                                      url.EndsWith("mercadolibre.com", StringComparison.OrdinalIgnoreCase);

    public ProductScrapedDto ParseHtml(string htmlContent, string url)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        // Valores por defecto
        string title = "Título no encontrado";
        decimal price = 0m;
        string currency = DetectCurrencyFromUrl(url); // Mismo helper que Amazon
        bool isAvailable = false;

        // 1. EXTRAER TÍTULO
        // Mercado Libre usa h1 con la clase ui-pdp-title consistentemente
        var titleNode = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'ui-pdp-title')]");
        if (titleNode != null)
        {
            title = titleNode.InnerText.Trim();
        }

        // 2. EXTRAER PRECIO (Estrategia Microdata)
        // Buscamos el tag <meta itemprop="price">. 
        // Es mucho más seguro que buscar clases CSS visuales.
        var priceMetaNode = doc.DocumentNode.SelectSingleNode("//meta[@itemprop='price']");

        if (priceMetaNode != null)
        {
            // El atributo content trae el número limpio: "5899" o "5899.00"
            var priceText = priceMetaNode.GetAttributeValue("content", "0");

            if (decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedPrice))
            {
                price = parsedPrice;
            }
        }
        else
        {
            // Fallback visual por si quitan el meta tag (poco probable en ML)
            price = ParsePriceVisual(doc);
        }

        // 3. DISPONIBILIDAD
        // En ML, si la publicación está pausada, suele aparecer un aviso.
        // Si hay precio y no hay mensaje de "Publicación pausada", asumimos stock.
        // También buscamos el botón de comprar.
        var buyButton =
            doc.DocumentNode.SelectSingleNode("//button[contains(@class, 'ui-pdp-actions__button')]") // Botón clásico
            ?? doc.DocumentNode.SelectSingleNode(
                "//a[contains(@href, 'buybox-form')]"); // Enlace de compra (presente en tu HTML)

        // Si encontramos precio (>0) y encontramos algún indicio de botón de compra/acción
        if (price > 0 && buyButton != null)
        {
            isAvailable = true;
        }

        // Verificación extra: Mensaje de pausado
        var pausedMessage =
            doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'ui-pdp-promotions-pill-label--PAUSED')]");
        if (pausedMessage != null) isAvailable = false;

        return new ProductScrapedDto(
            Title: title,
            Price: price,
            Currency: currency,
            IsAvailable: isAvailable,
            Url: url,
            Vendor: "MercadoLibre"
        );
    }

    private decimal ParsePriceVisual(HtmlDocument doc)
    {
        // Estrategia de respaldo buscando la estructura visual de ML
        // Buscamos el precio principal (el que tiene itemprop="offers" suele ser el contenedor)
        var priceContainer = doc.DocumentNode.SelectSingleNode(
            "//div[contains(@class, 'ui-pdp-price__second-line')]//span[@class='andes-money-amount__fraction']");

        if (priceContainer != null)
        {
            // ML usa puntos para miles en MX "5.899", hay que limpiarlo
            var rawText = priceContainer.InnerText.Replace(".", "").Replace(",", "").Trim();
            if (decimal.TryParse(rawText, out decimal result))
            {
                return result;
            }
        }

        return 0m;
    }

    // Helper reutilizable (idealmente iría en una clase base abstracta BaseScraperStrategy)
    private string DetectCurrencyFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLower();

            if (host.EndsWith(".com.mx")) return "MXN";
            if (host.EndsWith(".com.ar")) return "ARS";
            if (host.EndsWith(".com.co")) return "COP";
            if (host.EndsWith(".cl")) return "CLP";
            if (host.EndsWith(".br")) return "BRL";

            return "MXN"; // Default razonable para tu caso
        }
        catch
        {
            return "MXN";
        }
    }
}
