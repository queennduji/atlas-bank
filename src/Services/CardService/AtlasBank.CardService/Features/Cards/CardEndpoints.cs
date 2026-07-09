using AtlasBank.CardService.Data.Repositories;
using AtlasBank.CardService.Domain.Entities;
using AtlasBank.CardService.Infrastructure;
using AtlasBank.Shared.Messaging.Events;
using FluentValidation;
using MassTransit;

namespace AtlasBank.CardService.Features.Cards;

public static class CardEndpoints
{
    public static void MapCardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cards").RequireAuthorization();

        group.MapPost("/", IssueCard);
        group.MapGet("/{id:guid}", GetById);
        group.MapGet("/account/{accountId:guid}", GetByAccount);
        group.MapPost("/{id:guid}/freeze", Freeze);
        group.MapPost("/{id:guid}/unfreeze", Unfreeze);
        group.MapPut("/{id:guid}/spendingLimit", UpdateSpendingLimit);
    }

    private static async Task<IResult> IssueCard(
        IssueCardRequest request,
        IValidator<IssueCardRequest> validator,
        IAccountServiceClient accountClient,
        ICustomerServiceClient customerClient,
        ICardRepository repo,
        IPublishEndpoint publishEndpoint,
        CancellationToken ct)
    {
        var validationResult = await ValidationHelper.ValidateAsync(validator, request, ct);
        if (validationResult is not null) return validationResult;

        var account = await accountClient.GetByIdAsync(request.AccountId, ct);
        if (account is null) return Results.NotFound(new { message = "Account not found." });

        var customer = await customerClient.GetByIdAsync(account.CustomerId, ct);
        if (customer is null) return Results.NotFound(new { message = "Customer not found." });

        var cardHolderName = $"{customer.FirstName} {customer.LastName}";
        var card = Card.Issue(request.AccountId, account.CustomerId, cardHolderName, request.Type, request.SpendingLimit);
        await repo.AddAsync(card, ct);
        await repo.SaveChangesAsync(ct);

        await publishEndpoint.Publish(new CardIssuedEvent(
            card.Id, card.AccountId, card.CustomerId,
            card.MaskedCardNumber, card.CardHolderName,
            card.Type.ToString(), card.ExpiryDate, card.CreatedAt), ct);

        return Results.Created($"/api/cards/{card.Id}", ToResponse(card));
    }

    private static async Task<IResult> GetById(Guid id, ICardRepository repo, CancellationToken ct)
    {
        var card = await repo.GetByIdAsync(id, ct);
        return card is null ? Results.NotFound() : Results.Ok(ToResponse(card));
    }

    private static async Task<IResult> GetByAccount(Guid accountId, ICardRepository repo, CancellationToken ct)
    {
        var cards = await repo.GetByAccountIdAsync(accountId, ct);
        return Results.Ok(cards.Select(ToResponse));
    }

    private static async Task<IResult> Freeze(Guid id, ICardRepository repo, CancellationToken ct)
    {
        var card = await repo.GetByIdAsync(id, ct);
        if (card is null) return Results.NotFound();

        try { card.Freeze(); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }

        await repo.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(card));
    }

    private static async Task<IResult> Unfreeze(Guid id, ICardRepository repo, CancellationToken ct)
    {
        var card = await repo.GetByIdAsync(id, ct);
        if (card is null) return Results.NotFound();

        try { card.Unfreeze(); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }

        await repo.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(card));
    }

    private static async Task<IResult> UpdateSpendingLimit(
        Guid id,
        UpdateSpendingLimitRequest request,
        IValidator<UpdateSpendingLimitRequest> validator,
        ICardRepository repo,
        CancellationToken ct)
    {
        var validationResult = await ValidationHelper.ValidateAsync(validator, request, ct);
        if (validationResult is not null) return validationResult;

        var card = await repo.GetByIdAsync(id, ct);
        if (card is null) return Results.NotFound();

        try { card.UpdateSpendingLimit(request.SpendingLimit); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }

        await repo.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(card));
    }

    private static CardResponse ToResponse(Card card) => new(
        card.Id, card.AccountId, card.CustomerId,
        card.MaskedCardNumber, card.CardHolderName,
        card.Type, card.Status, card.SpendingLimit,
        card.ExpiryDate, card.CreatedAt, card.UpdatedAt);
}
