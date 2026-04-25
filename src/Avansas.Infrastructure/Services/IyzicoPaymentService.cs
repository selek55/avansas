using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Enums;
using Avansas.Domain.Interfaces;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AppInstallmentDetail = Avansas.Application.Interfaces.InstallmentDetail;
using IyzAddress = Iyzipay.Model.Address;

namespace Avansas.Infrastructure.Services;

public class IyzicoPaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly ILogger<IyzicoPaymentService> _logger;
    private readonly Options _iyzicoOptions;

    public IyzicoPaymentService(IUnitOfWork unitOfWork, IConfiguration config, ILogger<IyzicoPaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _config = config;
        _logger = logger;
        _iyzicoOptions = new Options
        {
            ApiKey = _config["Iyzico:ApiKey"] ?? string.Empty,
            SecretKey = _config["Iyzico:SecretKey"] ?? string.Empty,
            BaseUrl = _config["Iyzico:BaseUrl"] ?? "https://sandbox-api.iyzipay.com"
        };
    }

    public async Task<PaymentInitResult> InitiatePaymentAsync(InitiatePaymentDto dto)
    {
        var order = await _unitOfWork.Orders.Query()
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

        if (order == null)
            return new PaymentInitResult(false, null, null, "Sipariş bulunamadı.");

        var conversationId = Guid.NewGuid().ToString("N")[..12];

        var transaction = new PaymentTransaction
        {
            OrderId = dto.OrderId,
            ConversationId = conversationId,
            Amount = order.Total,
            Currency = "TRY",
            Installment = dto.Installment,
            PaymentMethod = Domain.Enums.PaymentMethod.CreditCard,
            Status = PaymentTransactionStatus.Initiated
        };
        await _unitOfWork.PaymentTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            var request = new CreatePaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = conversationId,
                Price = order.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                PaidPrice = order.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Currency = Currency.TRY.ToString(),
                Installment = dto.Installment,
                BasketId = order.OrderNumber,
                PaymentChannel = PaymentChannel.WEB.ToString(),
                PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                CallbackUrl = _config["Iyzico:CallbackUrl"] ?? "https://localhost:5001/odeme/3d-dogrulama",
                PaymentCard = new PaymentCard
                {
                    CardHolderName = dto.CardHolderName,
                    CardNumber = dto.CardNumber,
                    ExpireMonth = dto.ExpireMonth,
                    ExpireYear = dto.ExpireYear,
                    Cvc = dto.Cvc,
                    RegisterCard = 0
                },
                Buyer = new Buyer
                {
                    Id = dto.UserId,
                    Name = order.User.FirstName,
                    Surname = order.User.LastName,
                    GsmNumber = order.ShippingPhone,
                    Email = order.User.Email!,
                    IdentityNumber = "11111111111",
                    Ip = dto.UserIp,
                    RegistrationDate = order.User.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    LastLoginDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    RegistrationAddress = order.ShippingAddress,
                    City = order.ShippingCity,
                    Country = "Turkey"
                },
                ShippingAddress = new IyzAddress
                {
                    ContactName = $"{order.ShippingFirstName} {order.ShippingLastName}",
                    City = order.ShippingCity,
                    Country = "Turkey",
                    Description = order.ShippingAddress
                },
                BillingAddress = new IyzAddress
                {
                    ContactName = $"{order.BillingFirstName} {order.BillingLastName}",
                    City = order.BillingCity,
                    Country = "Turkey",
                    Description = order.BillingAddress
                },
                BasketItems = order.Items.Select((item, idx) => new BasketItem
                {
                    Id = item.Id.ToString(),
                    Name = item.ProductName,
                    Category1 = "Genel",
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = item.TotalPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                }).ToList()
            };

            var result = await ThreedsInitialize.Create(request, _iyzicoOptions);

            if (result.Status == "success")
            {
                transaction.Status = PaymentTransactionStatus.Pending3DSecure;
                _unitOfWork.PaymentTransactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();
                return new PaymentInitResult(true, conversationId, result.HtmlContent, null, transaction.Id);
            }

            transaction.Status = PaymentTransactionStatus.Failed;
            transaction.ErrorCode = result.ErrorCode;
            transaction.ErrorMessage = result.ErrorMessage;
            _unitOfWork.PaymentTransactions.Update(transaction);
            await _unitOfWork.SaveChangesAsync();
            return new PaymentInitResult(false, null, null, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iyzico 3DS başlatma hatası: OrderId={OrderId}", dto.OrderId);
            return new PaymentInitResult(false, null, null, "Ödeme sistemi hatası.");
        }
    }

    public async Task<PaymentResult> Handle3DSecureCallbackAsync(string conversationId, string token)
    {
        var transaction = await _unitOfWork.PaymentTransactions.Query()
            .FirstOrDefaultAsync(t => t.ConversationId == conversationId);

        if (transaction == null)
            return new PaymentResult(false, 0, null, 0, "NOT_FOUND", "İşlem bulunamadı.");

        try
        {
            var request = new CreateThreedsPaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = conversationId,
                PaymentId = token
            };

            var result = await ThreedsPayment.Create(request, _iyzicoOptions);

            if (result.Status == "success")
            {
                transaction.Status = PaymentTransactionStatus.Success;
                transaction.TransactionId = result.PaymentId;
                transaction.PaidAmount = decimal.Parse(result.PaidPrice ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                transaction.CardLastFour = result.LastFourDigits;
                transaction.CardAssociation = result.CardAssociation;
                transaction.CardFamily = result.CardFamily;
                transaction.BinNumber = result.BinNumber;
                _unitOfWork.PaymentTransactions.Update(transaction);

                var order = await _unitOfWork.Orders.GetByIdAsync(transaction.OrderId);
                if (order != null)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.PaymentTransactionId = result.PaymentId;
                    _unitOfWork.Orders.Update(order);
                }

                await _unitOfWork.SaveChangesAsync();
                return new PaymentResult(true, transaction.OrderId, result.PaymentId, transaction.PaidAmount, null, null);
            }

            transaction.Status = PaymentTransactionStatus.Failed;
            transaction.ErrorCode = result.ErrorCode;
            transaction.ErrorMessage = result.ErrorMessage;
            _unitOfWork.PaymentTransactions.Update(transaction);
            await _unitOfWork.SaveChangesAsync();
            return new PaymentResult(false, transaction.OrderId, null, 0, result.ErrorCode, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iyzico 3DS callback hatası: ConversationId={Id}", conversationId);
            return new PaymentResult(false, transaction.OrderId, null, 0, "EXCEPTION", ex.Message);
        }
    }

    public async Task<PaymentResult> ProcessDirectPaymentAsync(InitiatePaymentDto dto)
    {
        return await Task.FromResult(new PaymentResult(false, dto.OrderId, null, 0, "NOT_SUPPORTED", "Doğrudan ödeme desteği aktif değil."));
    }

    public async Task<RefundResult> RefundPaymentAsync(int orderId, decimal? amount = null)
    {
        var transaction = await GetTransactionByOrderIdAsync(orderId);
        if (transaction == null)
            return new RefundResult(false, 0, "Ödeme işlemi bulunamadı.");

        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            var refundAmount = amount ?? transaction.PaidAmount;

            var request = new CreateRefundRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = Guid.NewGuid().ToString("N")[..12],
                PaymentTransactionId = transaction.TransactionId!,
                Price = refundAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Currency = Currency.TRY.ToString(),
                Ip = "127.0.0.1"
            };

            var result = await Refund.Create(request, _iyzicoOptions);

            if (result.Status == "success")
            {
                transaction.Status = amount.HasValue ? PaymentTransactionStatus.PartialRefund : PaymentTransactionStatus.Refunded;
                _unitOfWork.PaymentTransactions.Update(transaction);

                if (order != null)
                {
                    order.PaymentStatus = amount.HasValue ? PaymentStatus.PartialRefund : PaymentStatus.Refunded;
                    _unitOfWork.Orders.Update(order);
                }

                await _unitOfWork.SaveChangesAsync();
                return new RefundResult(true, refundAmount, null);
            }

            return new RefundResult(false, 0, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iyzico iade hatası: OrderId={Id}", orderId);
            return new RefundResult(false, 0, ex.Message);
        }
    }

    public async Task<PaymentTransaction?> GetTransactionByOrderIdAsync(int orderId)
        => await _unitOfWork.PaymentTransactions.Query()
            .Where(t => t.OrderId == orderId && t.Status == PaymentTransactionStatus.Success)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<InstallmentInfoResult> GetInstallmentInfoAsync(string binNumber, decimal price)
    {
        try
        {
            var request = new RetrieveInstallmentInfoRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = Guid.NewGuid().ToString("N")[..12],
                BinNumber = binNumber,
                Price = price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            };

            var result = await InstallmentInfo.Retrieve(request, _iyzicoOptions);

            if (result.Status == "success" && result.InstallmentDetails?.Any() == true)
            {
                var details = result.InstallmentDetails[0].InstallmentPrices?
                    .Select(p => new AppInstallmentDetail(
                        p.InstallmentNumber ?? 1,
                        decimal.Parse(p.TotalPrice ?? "0", System.Globalization.CultureInfo.InvariantCulture),
                        decimal.Parse(p.Price ?? "0", System.Globalization.CultureInfo.InvariantCulture)))
                    .ToList() ?? new List<AppInstallmentDetail>();

                return new InstallmentInfoResult(true, details, null);
            }

            return new InstallmentInfoResult(false, new List<AppInstallmentDetail>(), result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Taksit bilgisi alınamadı: BIN={Bin}", binNumber);
            return new InstallmentInfoResult(false, new List<AppInstallmentDetail>(), ex.Message);
        }
    }
}
