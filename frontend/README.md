# AtlasBank Frontend

A React + TypeScript client for AtlasBank – sign up, open accounts, move money, issue
cards, and pull statements against the real gateway/microservices backend.

## Stack

- **React 19** + **TypeScript**, built with **Vite**
- **Tailwind CSS v4** for styling (CSS-first config, light/dark theme via `.dark` class)
- **React Router** for routing
- **TanStack Query** for server state (caching, invalidation after mutations)
- **react-hook-form** + **zod** for form validation
- **react-oidc-context** / **oidc-client-ts** for Keycloak login (Authorization Code + PKCE)

## Prerequisites

The backend needs to be running – from the repo root:

```bash
docker-compose up --build
```

This brings up Keycloak (`localhost:8080`), the API Gateway (`localhost:5000`), and the
core services. The `atlas-bank-app` Keycloak client is already configured (see
`keycloak/realm-export.json`) with `http://localhost:3000` as an allowed redirect/origin,
and the gateway's CORS policy already allows `localhost:3000` – so nothing on the backend
needs to change to run this frontend locally.

## Getting started

Two ways to run it, pick whichever fits what you're doing:

**Dev server (hot reload)** – best while actively changing code:

```bash
npm install
npm run dev
```

**Docker (production-style build)** – no Node needed locally; matches how the rest of
the stack runs. `docker-compose up --build` from the repo root builds and starts this
along with everything else, or build just this service:

```bash
docker-compose up --build frontend
```

Either way it's on `http://localhost:3000` (fixed – matches the Keycloak redirect URI).
The Docker build serves the static `dist/` output via nginx, with client-side routes
falling back to `index.html` (see `nginx.conf`) so deep links like `/dashboard` work on
refresh. Note that `VITE_*` vars are baked into the JS bundle at *build* time for the
Docker path (see `Dockerfile`'s `ARG`s / `docker-compose.yml`'s `build.args`) – changing
`.env` alone won't affect an already-built image, you'd need to rebuild.

## Configuration

Copy `.env.example` to `.env` to override defaults:

```
VITE_GATEWAY_URL=http://localhost:5000
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=atlas-bank
VITE_KEYCLOAK_CLIENT_ID=atlas-bank-app
```

## Structure

```
src/
  api/         Typed fetch client + TanStack Query hooks, one module per service
  auth/        Keycloak/OIDC wiring (AuthRoot, ProtectedRoute, token sync)
  components/  Reusable UI kit (components/ui) and app shell/nav (components/layout)
  pages/       One file per route
  lib/         Formatting, class-name, and theme helpers
```

## A note on enums

`AccountService`, `TransactionService`, and `CustomerService` serialize their enums as
raw integers (no `JsonStringEnumConverter` registered), while `CardService` and
`StatementService` serialize theirs as strings. `src/api/types.ts` documents this and
maps the numeric ones back to labels – worth standardizing on the backend at some point.

## Build

```bash
npm run build
```

Type-checks with `tsc -b` and produces a production bundle in `dist/`.
