using AtlasBank.Clients.Core.Models;

namespace AtlasBank.Clients.Core.Api;

public sealed partial class AtlasApiClient
{
    public Task<IReadOnlyList<StatementSummary>> GetAccountStatementsAsync(Guid accountId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<StatementSummary>>($"/api/statements/account/{accountId}", ct);

    public Task<Statement> GetStatementAsync(Guid statementId, CancellationToken ct = default) =>
        GetAsync<Statement>($"/api/statements/{statementId}", ct);

    public Task<Statement> GenerateStatementAsync(GenerateStatementRequest request, CancellationToken ct = default) =>
        PostAsync<Statement>("/api/statements/generate", request, ct);
}
