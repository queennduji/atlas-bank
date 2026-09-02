# AtlasBank.Maui

A .NET MAUI client for AtlasBank – sign up, open accounts, move money, issue cards, and pull
statements against the same API Gateway the [React frontend](../../../frontend) and the
[integration tests](../../../tests) exercise. Runs on Android, iOS, Mac Catalyst, and Windows
from one project.

This isn't a CRUD-over-HTTP toy: it's built to show the parts of MAUI development that don't
show up in a "Hello World" – a real OAuth2/PKCE login against Keycloak with no embedded
WebView, a resilient typed API client, MVVM with source-generated bindings, and platform-split
code kept to the minimum surface that actually needs it.

## Architecture

```
src/Clients/
  AtlasBank.Clients.Core/   Platform-agnostic: API client, DTOs, OIDC/PKCE auth, HTTP resilience
  AtlasBank.Maui/           MVVM UI: Views, ViewModels, Shell navigation, platform glue
tests/Clients/
  AtlasBank.Clients.Core.Tests/   xUnit + FluentAssertions over the auth/API logic above
```

**AtlasBank.Clients.Core has no MAUI reference.** Everything that doesn't need a UI
framework – the Gateway API client, the DTOs mirroring each service's wire contract, PKCE
code generation, the token refresh state machine, HTTP resilience – lives in a plain .NET
class library. AtlasBank.Wpf (planned, not built yet) will reference this same project
rather than duplicating it. It's also why the auth logic has real unit test coverage: none
of it depends on `SecureStorage`, `WebAuthenticator`, or anything else that only exists on
a device.

### Sign-in: Authorization Code + PKCE, no password ever touches this app

Keycloak's `atlas-bank-app` web client has the Resource Owner Password grant enabled
(`directAccessGrantsEnabled: true`) – it would be the *easiest* way to add login here: post
a username/password to a token endpoint, done. This app deliberately doesn't use it. Instead
it registers its own public client (`atlas-bank-maui`, PKCE-only, ROPC disabled – see
[`keycloak/realm-export.json`](../../../keycloak/realm-export.json)) and runs the real
browser-delegated flow:

- **Android / iOS / Mac Catalyst** – [`MobileOAuthBrowserLauncher`](Services/Auth/MobileOAuthBrowserLauncher.cs)
  hands the authorization URL to `WebAuthenticator`, which opens an OS-managed browser tab
  (Chrome Custom Tabs / `ASWebAuthenticationSession`) and catches the redirect via a
  registered `atlasbank://` URL scheme.
- **Windows** – [`LoopbackOAuthBrowserLauncher`](../AtlasBank.Clients.Core/Auth/LoopbackOAuthBrowserLauncher.cs)
  opens the system's default browser and catches the redirect on a local `HttpListener`
  instead – the same technique the Azure CLI, `gcloud`, and GitHub CLI use for desktop
  OAuth. It lives in `Clients.Core`, not this project, because it's plain BCL code with
  nothing MAUI-specific about it – AtlasBank.Wpf will use the identical class.

Both paths feed into [`OidcAuthenticator`](../AtlasBank.Clients.Core/Auth/OidcAuthenticator.cs),
which owns the actual PKCE challenge/verifier generation, state-parameter CSRF check, code
exchange, and the refresh-token rotation logic – the one piece of this app that's
security-sensitive enough to be worth testing in isolation, so it is
([`OidcAuthenticatorTests`](../../../tests/Clients/AtlasBank.Clients.Core.Tests/Auth/OidcAuthenticatorTests.cs)).

Tokens are held in `Microsoft.Maui.Storage.SecureStorage` (Android Keystore / iOS &
Mac Catalyst Keychain / Windows Credential Locker) via
[`MauiSecureTokenStore`](Services/Auth/MauiSecureTokenStore.cs) – never a plain file.

### API access

[`AtlasApiClient`](../AtlasBank.Clients.Core/Api/AtlasApiClient.cs) is a typed wrapper over
`HttpClient`, split into one partial class per resource (`.Customers.cs`, `.Accounts.cs`,
`.Transactions.cs`, `.Cards.cs`, `.Statements.cs`) mirroring how `frontend/src/api/` is
organized – the DTOs in `Models/` are a deliberate, field-for-field match of
`frontend/src/api/types.ts`, down to which enums serialize as numbers vs. strings depending
on the service (documented in [`Enums.cs`](../AtlasBank.Clients.Core/Models/Enums.cs)).

Every request goes through:

1. [`BearerTokenHandler`](../AtlasBank.Clients.Core/Http/BearerTokenHandler.cs) – attaches
   the current access token, and on a 401 forces one refresh-and-retry before giving up.
2. `Microsoft.Extensions.Http.Resilience`'s standard handler (retry with jittered backoff,
   a circuit breaker, per-attempt and total timeouts) – the same resilience package the
   API Gateway itself uses server-side (see `src/ApiGateway`), so a transient blip doesn't
   immediately surface as an error banner.

Deposits, withdrawals, and transfers carry an `Idempotency-Key` header exactly like the web
client (`frontend/src/api/transactions.ts`) – a retried request returns the original result
instead of moving money twice.

### MVVM

- **CommunityToolkit.Mvvm** source generators (`[ObservableProperty]`, `[RelayCommand]`) –
  no hand-written `INotifyPropertyChanged` boilerplate.
- **Shell navigation** with routes centralized in [`Routes.cs`](Services/Navigation/Routes.cs)
  and an `INavigationService` abstraction ([`ShellNavigationService.cs`](Services/Navigation/ShellNavigationService.cs))
  so ViewModels depend on an interface, not `Shell.Current`.
- Every ViewModel funnels its commands through `ViewModelBase.RunAsync`, which centralizes
  busy-state and error handling – and deliberately does **not** use `ConfigureAwait(false)`
  once a `Task` is about to touch a bound property. Continuations need to land back on the
  UI thread's `SynchronizationContext` to update bindings safely, same as in WPF; opting out
  of that with `ConfigureAwait(false)` would resume on a thread-pool thread instead.
- Full DI via `MauiProgram.cs` – pages, ViewModels, and services are constructor-injected,
  including into Shell-instantiated pages (`ContentTemplate="{DataTemplate views:LoginPage}"`)
  and `Routing.RegisterRoute` targets.

### Offline read cache

[`JsonFileOfflineCache`](Services/Offline/JsonFileOfflineCache.cs) persists the last
successful `/api/accounts/me` response to app-local storage. If a refresh fails and there's
nothing on screen yet, the dashboard falls back to that cached snapshot with a "showing
balances saved at …" banner rather than an empty screen – but only when there's nothing
live to show; a failed *refresh* over already-loaded data surfaces as a normal error instead
of silently overwriting good data with a stale cache. Deliberately narrow in scope: nothing
here queues offline writes. Deposits, transfers, and card actions still require connectivity,
same as the web app.

## Prerequisites

The backend needs to be running – from the repo root:

```bash
docker-compose up --build
```

This brings up Keycloak (`localhost:8080`) and the API Gateway (`localhost:5000`). The
`atlas-bank-maui` Keycloak client is already configured in `keycloak/realm-export.json`
with both the `atlasbank://callback` mobile redirect and the
`http://127.0.0.1:51739/*` Windows loopback redirect – nothing on the backend needs to
change to run this app.

> **Android emulator note:** the emulator's virtual network maps the *host machine's*
> localhost to `10.0.2.2`, not `127.0.0.1` – see [`AppConfig.cs`](Config/AppConfig.cs), which
> switches the Gateway/Keycloak URLs based on target platform.

## Running it

```bash
dotnet workload restore
dotnet build -f net10.0-windows10.0.19041.0   # or net10.0-android / net10.0-ios / net10.0-maccatalyst
```

Or open `AtlasBank.slnx` in Visual Studio / Rider and pick a target from the run dropdown.

## Testing

```bash
dotnet test tests/Clients/AtlasBank.Clients.Core.Tests
```

Unit tests cover PKCE generation (checked against the RFC 7636 Appendix B test vector), the
full sign-in/refresh/sign-out state machine against a fake browser launcher and a fake
`HttpMessageHandler`, and `AtlasApiClient`'s error-message extraction (including the ASP.NET
`ValidationProblem` shape) – all without touching a device, an emulator, or the network.
CI (`.github/workflows/maui-ci.yml`) runs this job on every push, plus a Windows/Android
build matrix. iOS/Mac Catalyst aren't built in CI since that needs a macOS runner with
Xcode – out of scope for this project, but both targets build locally the same way Windows
and Android do.

## What's next

AtlasBank.Wpf will reuse `AtlasBank.Clients.Core` as-is – same API client, same
`OidcAuthenticator`, same `LoopbackOAuthBrowserLauncher` – supplying only a WPF-flavored
`ITokenStore` (DPAPI-protected file) and its own Views/ViewModels.
