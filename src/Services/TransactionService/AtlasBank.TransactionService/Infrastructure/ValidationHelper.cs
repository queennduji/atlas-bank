using FluentValidation;

namespace AtlasBank.TransactionService.Infrastructure;

public static class ValidationHelper
{
    public static async Task<IResult?> ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken ct = default)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (result.IsValid) return null;

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }
}
