using Avansas.Application.DTOs;
using System.Text.Json;

namespace Avansas.Web.Helpers;

public static class JsonLdHelper
{
    public static string Organization(string name, string url, string phone, string email) =>
        Serialize(new
        {
            @context = "https://schema.org",
            @type = "Organization",
            name,
            url,
            contactPoint = new { @type = "ContactPoint", telephone = phone, email, contactType = "customer service" }
        });

    public static string Product(ProductDto product, string siteUrl, double rating, int reviewCount) =>
        Serialize(new
        {
            @context = "https://schema.org",
            @type = "Product",
            name = product.Name,
            description = product.ShortDescription ?? product.Name,
            image = string.IsNullOrEmpty(product.MainImageUrl) ? null : $"{siteUrl}{product.MainImageUrl}",
            sku = product.SKU,
            offers = new
            {
                @type = "Offer",
                price = (product.DiscountedPrice ?? product.Price).ToString("0.00"),
                priceCurrency = "TRY",
                availability = product.StockQuantity > 0
                    ? "https://schema.org/InStock"
                    : "https://schema.org/OutOfStock",
                url = $"{siteUrl}/urun/{product.Slug}"
            },
            aggregateRating = reviewCount > 0 ? new
            {
                @type = "AggregateRating",
                ratingValue = rating.ToString("0.0"),
                reviewCount
            } : null
        });

    public static string BreadcrumbList(List<(string Name, string Url)> items) =>
        Serialize(new
        {
            @context = "https://schema.org",
            @type = "BreadcrumbList",
            itemListElement = items.Select((item, i) => new
            {
                @type = "ListItem",
                position = i + 1,
                name = item.Name,
                item = item.Url
            }).ToList()
        });

    public static string FaqPage(List<ProductQuestionDto> questions) =>
        Serialize(new
        {
            @context = "https://schema.org",
            @type = "FAQPage",
            mainEntity = questions
                .Where(q => !string.IsNullOrEmpty(q.AnswerText))
                .Select(q => new
                {
                    @type = "Question",
                    name = q.QuestionText,
                    acceptedAnswer = new { @type = "Answer", text = q.AnswerText }
                }).ToList()
        });

    private static string Serialize(object obj) =>
        JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
}
