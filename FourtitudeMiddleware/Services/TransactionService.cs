using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FourtitudeMiddleware.Dtos;
using Microsoft.Extensions.Logging;
using FluentValidation;

namespace FourtitudeMiddleware.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IPartnerService _partnerService;
        private readonly ILogger<TransactionService> _logger;
        private readonly IValidator<SubmitTransactionRequest> _validator;

        public TransactionService(
            IPartnerService partnerService, 
            ILogger<TransactionService> logger,
            IValidator<SubmitTransactionRequest> validator)
        {
            _partnerService = partnerService;
            _logger = logger;
            _validator = validator;
        }

        public ServiceResponse<SubmitTransactionResponse> ProcessTransaction(SubmitTransactionRequest request)
        {
            var response = new ServiceResponse<SubmitTransactionResponse>();
            try
            {
                // Validate request using FluentValidation
                var validationResult = _validator.Validate(request);
                if (!validationResult.IsValid)
                {
                    response.ResultMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return response;
                }

                // Validate partner credentials
                if (!_partnerService.ValidatePartner(request.PartnerKey, request.PartnerPassword))
                {
                    response.ResultMessage = "Invalid partner credentials";
                    return response;
                }

                // Validate timestamp format
                if (!DateTime.TryParse(request.Timestamp, out _))
                {
                    response.ResultMessage = "Invalid timestamp format";
                    return response;
                }

                // Create parameters dictionary for signature validation
                var sigParams = new Dictionary<string, string>
                {
                    { "partnerkey", request.PartnerKey },
                    { "partnerrefno", request.PartnerRefNo },
                    { "partnerpassword", request.PartnerPassword },
                    { "totalamount", request.TotalAmount.ToString() }
                };

                // Add items if present
                if (request.Items != null && request.Items.Any())
                {
                    sigParams.Add("items", JsonSerializer.Serialize(request.Items));
                }

                // Format timestamp for signature validation (yyyyMMddHHmmss)
                string sigTimestamp = DateTime.Parse(request.Timestamp).ToString("yyyyMMddHHmmss");

                // Validate signature
                if (!_partnerService.ValidateSignature(sigParams, sigTimestamp, request.Sig))
                {
                    response.ResultMessage = "Invalid signature";
                    return response;
                }

                // Process transaction (in a real implementation, this would involve more business logic)
                long totalAmount = request.TotalAmount;
                long totalDiscount = 0; // In a real scenario, this would be calculated based on business rules
                long finalAmount = totalAmount - totalDiscount;

                response.Result = 1;
                response.Data = new SubmitTransactionResponse
                {
                    TotalAmount = totalAmount,
                    TotalDiscount = totalDiscount,
                    FinalAmount = finalAmount
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction");
                response.ResultMessage = "Internal server error";
                return response;
            }
        }
    }
} 