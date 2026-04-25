namespace Avansas.Application.Interfaces;

public interface IInvoiceService
{
    Task<byte[]> GenerateOrderInvoicePdfAsync(int orderId);
    Task<byte[]> GenerateReturnInvoicePdfAsync(int returnRequestId);
    Task<byte[]> GenerateProformaInvoicePdfAsync(int orderId);
    string GetInvoiceNumber(int orderId);
}
