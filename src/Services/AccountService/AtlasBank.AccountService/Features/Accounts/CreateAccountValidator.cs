using AtlasBank.AccountService.Domain.Enums;
using FluentValidation;

namespace AtlasBank.AccountService.Features.Accounts;

public class CreateAccountValidator : AbstractValidator<CreateAccountRequest>
{
    private static readonly HashSet<string> ValidCurrencies = ["USD", "EUR", "GBP", "CAD", "AUD", "JPY", "CHF", "NGN"];

    public CreateAccountValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Account type must be 0 (Checking) or 1 (Savings).");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.")
            .Must(c => ValidCurrencies.Contains(c.ToUpper())).WithMessage($"Currency must be one of: {string.Join(", ", ValidCurrencies)}.");
    }
}
