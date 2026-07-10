using FluentValidation;

namespace AtlasBank.TransactionService.Features.Transactions;

public class DepositValidator : AbstractValidator<DepositRequest>
{
    public DepositValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}

public class WithdrawValidator : AbstractValidator<WithdrawRequest>
{
    public WithdrawValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}

public class TransferValidator : AbstractValidator<TransferRequest>
{
    public TransferValidator()
    {
        RuleFor(x => x.FromAccountId).NotEmpty();
        RuleFor(x => x.ToAccountId).NotEmpty();
        RuleFor(x => x.FromAccountId).NotEqual(x => x.ToAccountId).WithMessage("Cannot transfer to the same account.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}
