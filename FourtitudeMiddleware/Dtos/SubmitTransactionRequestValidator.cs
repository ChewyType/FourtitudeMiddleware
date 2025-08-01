using FluentValidation;
using FourtitudeMiddleware.Helpers;
using System;
using System.Linq;

namespace FourtitudeMiddleware.Dtos
{
    public class SubmitTransactionRequestValidator : AbstractValidator<SubmitTransactionRequest>
    {
        public SubmitTransactionRequestValidator()
        {
            // Required fields with custom error messages
            RuleFor(x => x.PartnerKey)
                .NotEmpty()
                .WithMessage("partnerkey is required.")
                .MaximumLength(50)
                .WithMessage("partnerkey must not exceed 50 characters.");

            RuleFor(x => x.PartnerRefNo)
                .NotEmpty()
                .WithMessage("partnerrefno is required.")
                .MaximumLength(50)
                .WithMessage("partnerrefno must not exceed 50 characters.");

            RuleFor(x => x.PartnerPassword)
                .NotEmpty()
                .WithMessage("partnerpassword is required.")
                .MaximumLength(50)
                .WithMessage("partnerpassword must not exceed 50 characters.");

            RuleFor(x => x.Sig)
                .NotEmpty()
                .WithMessage("sig is required.");

            RuleFor(x => x.TotalAmount)
                .GreaterThan(0)
                .WithMessage("totalamount must be greater than 0.");

            // If items are provided, totalamount must match sum of itemDetails minus discount
            RuleFor(x => x)
                .Custom((request, context) =>
                {
                    if (request.Items != null && request.Items.Any())
                    {
                        var sum = request.Items.Sum(i => i.UnitPrice * i.Qty);
                        if (sum != request.TotalAmount)
                        {
                            context.AddFailure("Invalid Total Amount.", "Only applicable when itemDetails is provided. The total value stated in itemDetails array not equal to value in totalamount.");
                        }
                    }
                });

            // Timestamp must be within ±5 minutes of server time
            RuleFor(x => x.Timestamp)
                .Custom((timestamp, context) =>
                {
                    const string iso8601Format = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";
                    if (string.IsNullOrEmpty(timestamp))
                    {
                        context.AddFailure("timestamp", "timestamp is required.");
                        return;
                    }

                    if (!DateTime.TryParseExact(timestamp, iso8601Format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsedTimestamp))
                    {
                        context.AddFailure("timestamp", $"Invalid timestamp format. Must be ISO 8601 UTC (e.g., 2024-08-15T02:11:22.0000000Z)");
                        return;
                    }

                    var now = DateTime.UtcNow;
                    var diff = Math.Abs((now - parsedTimestamp.ToUniversalTime()).TotalMinutes);
                    if (diff > 5)
                    {
                        context.AddFailure("Expired.", $"Provided timestamp exceed server time ±5min. The valid time will be ±5 Min of the server time. Server time: {now:yyyy-MM-ddTHH:mm:ss.fffffffZ}");
                    }
                });

            RuleForEach(x => x.Items).SetValidator(new ItemRequestValidator());
        }
    }

    public class ItemRequestValidator : AbstractValidator<ItemRequest>
    {
        public ItemRequestValidator()
        {
            RuleFor(x => x.PartnerItemRef)
                .NotEmpty()
                .WithMessage("partneritemref is required.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("name is required.");

            RuleFor(x => x.Qty)
                .GreaterThan(0)
                .LessThanOrEqualTo(5)
                .WithMessage("qty must be between 1 and 5.");

            RuleFor(x => x.UnitPrice)
                .GreaterThan(0)
                .WithMessage("unitprice must be greater than 0.");
        }
    }
} 