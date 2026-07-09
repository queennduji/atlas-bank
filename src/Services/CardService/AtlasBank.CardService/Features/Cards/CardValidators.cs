using FluentValidation;

namespace AtlasBank.CardService.Features.Cards;

public class IssueCardValidator : AbstractValidator<IssueCardRequest>
{
    public IssueCardValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.SpendingLimit).GreaterThan(0).LessThanOrEqualTo(100_000);
    }
}

public class UpdateSpendingLimitValidator : AbstractValidator<UpdateSpendingLimitRequest>
{
    public UpdateSpendingLimitValidator()
    {
        RuleFor(x => x.SpendingLimit).GreaterThan(0).LessThanOrEqualTo(100_000);
    }
}
