using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FourtitudeMiddleware.Dtos;
using Microsoft.Extensions.Logging;
using FluentValidation;
using FourtitudeMiddleware.Services;
using static FourtitudeMiddleware.Helpers.NumberHelpers;
using log4net;
using System.Reflection;

namespace FourtitudeMiddleware.Services
{
    public class TransactionService : ITransactionService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
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
            log4net.Config.XmlConfigurator.Configure();
        }

        public ServiceResponse<SubmitTransactionResponse> ProcessTransaction(SubmitTransactionRequest request)
        {
            var response = new ServiceResponse<SubmitTransactionResponse>();
            try
            {
                // Clone and encrypt password for logging
                var logRequest = CloneAndEncryptPassword(request);
                log.Info($"Request: {JsonSerializer.Serialize(logRequest)}");

                // Validate request using FluentValidation
                var validationResult = _validator.Validate(request);
                if (!validationResult.IsValid)
                {
                    response.ResultMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    log.Warn($"Validation failed: {response.ResultMessage}");
                    return response;
                }

                // Validate partner credentials
                if (!_partnerService.ValidatePartner(request.PartnerKey, request.PartnerPassword))
                {
                    response.ResultMessage = "Invalid partner credentials";
                    log.Warn($"Invalid partner credentials for PartnerKey: {request.PartnerKey}");
                    return response;
                }

                // Validate timestamp format
                if (!DateTime.TryParse(request.Timestamp, out _))
                {
                    response.ResultMessage = "Invalid timestamp format";
                    log.Warn($"Invalid timestamp format: {request.Timestamp}");
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
                    log.Warn($"Invalid signature for PartnerKey: {request.PartnerKey}");
                    return response;
                }

                // Process transaction (in a real implementation, this would involve more business logic)
                long totalAmount = request.TotalAmount;

                var transactionResponse = new SubmitTransactionResponse();
                ApplyDiscounts(transactionResponse, totalAmount);

                response.Data = transactionResponse;
                log.Info($"Response: {JsonSerializer.Serialize(response)}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction");
                log.Error("Error processing transaction", ex);
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