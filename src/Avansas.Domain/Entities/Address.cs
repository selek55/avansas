namespace Avansas.Domain.Entities;

public class Address : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "Türkiye";
    public bool IsDefault { get; set; } = false;
    public bool IsBillingAddress { get; set; } = false;
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
