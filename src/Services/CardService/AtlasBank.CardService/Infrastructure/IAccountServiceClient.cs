namespace AtlasBank.CardService.Infrastructure;

public record AccountDto(Guid Id, Guid CustomerId, string AccountNumber, int Status, decimal Balance, string Currency);

public interface IAccountServiceClient
{
    Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default);
}
