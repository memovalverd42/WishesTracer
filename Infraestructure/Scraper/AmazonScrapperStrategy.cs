using System.Globalization;
using System.Text.Json.Nodes;
using HtmlAgilityPack;
using WishesTracer.Application.DTOs;

namespace WishesTracer.Infraestructure.Scraper;

public class AmazonStrategy : IScraperStrategy
{
    public bool CanHandle(string uri) => uri.Contains("amazon", StringComparison.OrdinalIgnoreCase) ||
                                      uri.EndsWith("amazon.com.mx", StringComparison.OrdinalIgnoreCase) ||
                                      uri.EndsWith("amazon.com", StringComparison.OrdinalIgnoreCase);

    public ProductScrapedDto ParseHtml(string htmlContent, string url)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        // Valores por defecto
        string title = "Título no encontrado";
        decimal price = 0m;

        // Inicializamos la moneda intentando adivinar por la URL primero (Nivel 2)
        // Esto sirve de "backup" si el JSON falla.
        string currency = DetectCurrencyFromUrl(url);

        bool isAvailable = false;

        // 1. EXTRAER TÍTULO
        var titleNode = doc.DocumentNode.SelectSingleNode("//span[@id='productTitle']");
        if (titleNode != null)
        {
            title = titleNode.InnerText.Trim();
        }

        // 2. EXTRAER PRECIO (Lógica Principal: JSON Oculto)
        var jsonPriceNode =
            doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'twister-plus-buying-options-price-data')]");
        bool priceFound = false;

        if (jsonPriceNode != null && !string.IsNullOrWhiteSpace(jsonPriceNode.InnerText))
        {
            try
            {
                var jsonNode = JsonNode.Parse(jsonPriceNode.InnerText);
                var priceData = jsonNode?["desktop_buybox_group_1"]?[0];

                if (priceData != null)
                {
                    price = (decimal)(priceData["priceAmount"] ?? 0m);

                    // --- AQUÍ ESTÁ EL TRUCO PARA EL ISO ---
                    // Buscamos "locale": "es-MX" en el JSON
                    string localeStr = (string)priceData["locale"];

                    if (!string.IsNullOrEmpty(localeStr))
                    {
                        // Convertimos "es-MX" a "MXN" automáticamente
                        currency = new RegionInfo(localeStr).ISOCurrencySymbol;
                    }
                    // -------------------------------------

                    priceFound = true;
                }
            }
            catch
            {
                Console.WriteLine("Warning: Falló el parseo JSON de Amazon, usando fallback visual.");
            }
        }

        // 3. EXTRAER PRECIO (Lógica Respaldo: Visual)
        if (!priceFound)
        {
            price = ParsePriceVisual(doc);
            // Si entramos aquí, 'currency' se queda con lo que adivinamos por la URL
        }

        // 4. DISPONIBILIDAD
        var availabilityNode = doc.DocumentNode.SelectSingleNode("//div[@id='availability']");
        var availabilityText = availabilityNode?.InnerText.Trim().ToLower() ?? "";

        isAvailable = price > 0 && !availabilityText.Contains("no disponible") &&
                      !availabilityText.Contains("currently unavailable");

        return new ProductScrapedDto(
            Title: title,
            Price: price,
            Currency: currency, // Ahora llevará "MXN" o "USD"
            IsAvailable: isAvailable,
            Url: url,
            Vendor: "Amazon"
        );
    }

    // Método auxiliar para adivinar moneda si falla el JSON
    private string DetectCurrencyFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLower();

            if (host.EndsWith(".com.mx")) return "MXN";
            if (host.EndsWith(".es")) return "EUR";
            if (host.EndsWith(".co.uk")) return "GBP";
            if (host.EndsWith(".br")) return "BRL";
            // ... puedes agregar más ...

            return "USD"; // Default para .com
        }
        catch
        {
            return "USD";
        }
    }

    private decimal ParsePriceVisual(HtmlDocument doc)
    {
        var wholePartNode =
            doc.DocumentNode.SelectSingleNode(
                "//div[@id='corePriceDisplay_desktop_feature_div']//span[contains(@class, 'a-price-whole')]");
        var fractionPartNode = doc.DocumentNode.SelectSingleNode(
            "//div[@id='corePriceDisplay_desktop_feature_div']//span[contains(@class, 'a-price-fraction')]");

        if (wholePartNode != null && fractionPartNode != null)
        {
            var wholeText = wholePartNode.InnerText.Replace(".", "").Replace(",", "").Trim();
            var fractionText = fractionPartNode.InnerText.Trim();
            string fullPrice = $"{wholeText}.{fractionText}";

            if (decimal.TryParse(fullPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
        }

        return 0m;
    }
}
