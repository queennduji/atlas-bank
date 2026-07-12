using FluentValidation;

namespace AtlasBank.StatementService.Features.Statements;

public class GenerateStatementRequestValidator : AbstractValidator<GenerateStatementRequest>
{
    public GenerateStatementRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.PeriodStart).NotEmpty();
        RuleFor(x => x.PeriodEnd).NotEmpty()
            .GreaterThan(x => x.PeriodStart).WithMessage("PeriodEnd must be after PeriodStart.");
        RuleFor(x => x).Must(x => x.PeriodEnd - x.PeriodStart <= TimeSpan.FromDays(366))
            .WithMessage("Statement period cannot exceed 1 year.");
    }
}
