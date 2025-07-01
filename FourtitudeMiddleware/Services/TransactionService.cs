using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FourtitudeMiddleware.Dtos;
using Microsoft.Extensions.Logging;

namespace FourtitudeMiddleware.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IPartnerService _partnerService;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(
            IPartnerService partnerService, 
            ILogger<TransactionService> logger)
        {
            _partnerService = partnerService;
            _logger = logger;
        }

        public ServiceResponse<SubmitTransactionResponse> ProcessTransaction(SubmitTransactionRequest request)
        {
            var response = new ServiceResponse<SubmitTransactionResponse>();
            try
            {
                // Validate request
                if (request == null)
                {
                    response.ResultMessage = "Invalid request format";
                    return response;
                }

                // Validate required fields
                if (string.IsNullOrEmpty(request.PartnerKey)
                    || string.IsNullOrEmpty(request.PartnerRefNo)
                    || string.IsNullOrEmpty(request.PartnerPassword)
                    || string.IsNullOrEmpty(request.Timestamp)
                    || string.IsNullOrEmpty(request.Sig)
                    || request.TotalAmount <= 0)
                {
                    response.ResultMessage = "Missing or invalid required fields";
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
                    response.Result = 0;
                    response.ResultMessage = "Invalid signature";
                    return response;
                }

                // Validate items if present
                if (request.Items != null)
                {
                    foreach (var item in request.Items)
                    {
                        if (string.IsNullOrEmpty(item.PartnerItemRef)
                            || string.IsNullOrEmpty(item.Name)
                            || item.Qty <= 0 || item.Qty > 5
                            || item.UnitPrice <= 0)
                        {
                            response.Result = 0;
                            response.ResultMessage = "Invalid item details";
                            return response;
                        }
                    }
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
                response.Result = 0;
                response.ResultMessage = "Internal server error";
                return response;
            }
        }
    }
} 