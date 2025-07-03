using System.Text.Json;
using FourtitudeMiddleware.Dtos;
using FluentValidation;
using static FourtitudeMiddleware.Helpers.NumberHelpers;
using FourtitudeMiddleware.Commons;

namespace FourtitudeMiddleware.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IPartnerService _partnerService;
        private readonly ILogger<TransactionService> _logger;
        private readonly IValidator<SubmitTransactionRequest> _validator;

        public TransactionService(
            IPartnerService partnerService, 
            IValidator<SubmitTransactionRequest> validator)
        {
            _partnerService = partnerService;
            _validator = validator;
        }

        public SubmitTransactionResponse ProcessTransaction(SubmitTransactionRequest request)
        {
            var response = new SubmitTransactionResponse();
            try
            {
                // Clone and encrypt password for logging
                var logRequest = CloneAndEncryptPassword(request);

                // Validate request using FluentValidation
                var validationResult = _validator.Validate(request);
                if (!validationResult.IsValid)
                {
                    response.Result = 0;
                    response.ResultMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return response;
                }

                // Validate partner credentials
                if (!_partnerService.ValidatePartner(request.PartnerRefNo, request.PartnerPassword))
                {
                    response.Result = 0;
                    response.ResultMessage = "Access Denied!";
                    return response;
                }

                // Validate timestamp format
                if (!DateTime.TryParse(request.Timestamp, out _))
                {
                    response.Result = 0;
                    response.ResultMessage = "Invalid timestamp format";
                    return response;
                }

                // Create parameters dictionary for signature validation
                var sigParams = new Dictionary<string, string>
                {
                    { DictionaryKeys.SignaturePartnerKey, request.PartnerKey },
                    { DictionaryKeys.SignaturePartnerRefNo, request.PartnerRefNo },
                    { DictionaryKeys.SignatureTotalAmount, request.TotalAmount.ToString() }
                };

                // Add items if present
                if (request.Items != null && request.Items.Any())
                {
                    sigParams.Add("items", JsonSerializer.Serialize(request.Items));
                }

                string sigTimestamp = DateTime.Parse(request.Timestamp).ToUniversalTime().ToString("o");

                // Validate signature
                if (!_partnerService.ValidateSignature(sigParams, sigTimestamp, request.Sig))
                {
                    response.Result = 0;
                    response.ResultMessage = "Invalid signature";
                    return response;
                }

                // Process transaction (in a real implementation, this would involve more business logic)
                long totalAmount = request.TotalAmount;

                ApplyDiscounts(response, totalAmount);
                response.Result = 1;
                return response;
            }
            catch (Exception)
            {
                response.Result = 0;
                response.ResultMessage = "Internal server error";
                return response;
            }
        }

        // Helper method for discount calculation
        private void ApplyDiscounts(SubmitTransactionResponse response, long totalAmount)
        {
            double baseDiscountPercent = 0;
            if (totalAmount >= 200 && totalAmount <= 500)
            {
                baseDiscountPercent = 0.05;
            }
            else if (totalAmount >= 501 && totalAmount <= 800)
            {
                baseDiscountPercent = 0.07;
            }
            else if (totalAmount >= 801 && totalAmount <= 1200)
            {
                baseDiscountPercent = 0.10;
            }
            else if (totalAmount > 1200)
            {
                baseDiscountPercent = 0.15;
            }

            double conditionalDiscountPercent = 0;
            // Prime number check (above 500)
            if (totalAmount > 500 && NumberHelper.IsPrime(totalAmount))
            {
                conditionalDiscountPercent += 0.08;
            }
            // Ends with 5 and above 900
            if (totalAmount > 900 && totalAmount % 10 == 5)
            {
                conditionalDiscountPercent += 0.10;
            }

            double totalDiscountPercent = baseDiscountPercent + conditionalDiscountPercent;
            if (totalDiscountPercent > 0.20)
            {
                totalDiscountPercent = 0.20;
            }

            long totalDiscount = (long)Math.Round(totalAmount * totalDiscountPercent);
            long finalAmount = totalAmount - totalDiscount;

            response.TotalAmount = totalAmount;
            response.TotalDiscount = totalDiscount;
            response.FinalAmount = finalAmount;
        }

        // Helper method to clone request and encrypt password for logging
        private SubmitTransactionRequest CloneAndEncryptPassword(SubmitTransactionRequest request)
        {
            return new SubmitTransactionRequest
            {
                PartnerKey = request.PartnerKey,
                PartnerRefNo = request.PartnerRefNo,
                PartnerPassword = EncryptForLog(request.PartnerPassword),
                TotalAmount = request.TotalAmount,
                Items = request.Items,
                Timestamp = request.Timestamp,
                Sig = request.Sig
            };
        }

        // Simple encryption for logging (for demonstration, use a real encryption in production)
        private string EncryptForLog(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainBytes.Reverse().ToArray()); // Simple reverse + base64
        }
    }
} 