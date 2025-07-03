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

                var (totalDiscount, finalAmount, totalDiscountPercent) = NumberHelper.CalculateDiscount(totalAmount);
                response.TotalAmount = totalAmount;
                response.TotalDiscount = totalDiscount;
                response.FinalAmount = finalAmount;
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
    }
} 