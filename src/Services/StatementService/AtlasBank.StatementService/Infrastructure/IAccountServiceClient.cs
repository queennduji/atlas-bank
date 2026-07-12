namespace AtlasBank.StatementService.Infrastructure;

public record AccountDto(Guid Id, Guid CustomerId, string AccountNumber, string Currency, decimal Balance);

public interface IAccountServiceClient
{
    Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default);
}
