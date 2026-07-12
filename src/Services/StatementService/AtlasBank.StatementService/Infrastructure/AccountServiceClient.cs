using AtlasBank.Grpc;

namespace AtlasBank.StatementService.Infrastructure;

public class AccountServiceClient(AccountGrpcService.AccountGrpcServiceClient grpcClient) : IAccountServiceClient
{
    public async Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default)
    {
        var reply = await grpcClient.GetAccountAsync(
            new GetAccountRequest { AccountId = accountId.ToString() }, cancellationToken: ct);
        if (!reply.Found) return null;
        return new AccountDto(
            Guid.Parse(reply.Id), Guid.Parse(reply.CustomerId),
            reply.AccountNumber, reply.Currency, (decimal)reply.Balance);
    }
}
