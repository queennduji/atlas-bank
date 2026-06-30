using FluentValidation;

namespace AtlasBank.CustomerService.Features.Customers;

public class RegisterCustomerValidator : AbstractValidator<RegisterCustomerRequest>
{
    public RegisterCustomerValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[1-9]\d{6,14}$").WithMessage("A valid international phone number is required.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))
            .WithMessage("You must be at least 18 years old to register.");

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
