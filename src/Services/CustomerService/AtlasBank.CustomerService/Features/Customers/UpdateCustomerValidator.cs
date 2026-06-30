using FluentValidation;

namespace AtlasBank.CustomerService.Features.Customers;

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[1-9]\d{6,14}$").WithMessage("A valid international phone number is required.");

        RuleFor(x => x.Address).NotNull().WithMessage("Address is required.")
            .ChildRules(address =>
            {
                address.RuleFor(x => x.Street).NotEmpty().WithMessage("Street is required.").MaximumLength(200);
                address.RuleFor(x => x.City).NotEmpty().WithMessage("City is required.").MaximumLength(100);
                address.RuleFor(x => x.State).NotEmpty().WithMessage("State is required.").MaximumLength(100);
                address.RuleFor(x => x.ZipCode).NotEmpty().WithMessage("Zip code is required.").MaximumLength(20);
                address.RuleFor(x => x.Country).NotEmpty().WithMessage("Country is required.").MaximumLength(100);
            });
    }
}
