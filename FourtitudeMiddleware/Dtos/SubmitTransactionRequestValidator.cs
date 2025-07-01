using FluentValidation;

namespace FourtitudeMiddleware.Dtos
{
    public class SubmitTransactionRequestValidator : AbstractValidator<SubmitTransactionRequest>
    {
        public SubmitTransactionRequestValidator()
        {
            RuleFor(x => x.PartnerKey).NotEmpty();
            RuleFor(x => x.PartnerRefNo).NotEmpty();
            RuleFor(x => x.PartnerPassword).NotEmpty();
            RuleFor(x => x.Timestamp).NotEmpty();
            RuleFor(x => x.Sig).NotEmpty();
            RuleFor(x => x.TotalAmount).GreaterThan(0);

            RuleForEach(x => x.Items).SetValidator(new ItemRequestValidator());
        }
    }

    public class ItemRequestValidator : AbstractValidator<ItemRequest>
    {
        public ItemRequestValidator()
        {
            RuleFor(x => x.PartnerItemRef).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Qty).GreaterThan(0).LessThanOrEqualTo(5);
            RuleFor(x => x.UnitPrice).GreaterThan(0);
        }
    }
} 