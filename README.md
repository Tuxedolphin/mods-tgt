# mod-together

A full-stack web app with a SvelteKit frontend (deployed on Cloudflare Workers) and an ASP.NET Core backend backed by Supabase (PostgreSQL). The live app can be viewed [here](https://mods-tgt.com).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) + [pnpm](https://pnpm.io/) (`npm i -g pnpm`)
- A [Supabase](https://supabase.com/) project

---

## Setup

### Backend

**1. Configure environment variables**

```shell
export Supabase__Url="https://<project-ref>.supabase.co"
export Supabase__PublishableKey="<your-anon-key>"
export ConnectionStrings__DefaultConnection="Host=aws-0-<region>.pooler.supabase.com;Database=postgres;Username=postgres.<project-ref>;Password=<password>;Port=5432;SSL Mode=Require;Trust Server Certificate=true"
export ASPNETCORE_ENVIRONMENT=Development
```

> **Note:** Use the session pooler host (`aws-0-*.pooler.supabase.com`) — the direct DB host is IPv6-only and will fail on most cloud/Linux environments. If your environment supports IPv6, you may use the direct DB host instead.

**2. Apply migrations**

```shell
cd backend
dotnet ef database update
```

**3. Run**

```shell
dotnet run
```

Dev Server runs at `http://localhost:5233`. Scalar API docs available at `/scalar` in Development.

---

### Frontend

**1. Install dependencies**

```shell
cd frontend
pnpm install
```

**2. Configure environment**

Create an `.env` file in the root of `frontend` with the following entry:

```shell
PUBLIC_DB_LINK=http://localhost:5233
```

The `http://localhost:5233` being the Dotnet backend url. In production, this is dynamically replaced with `api.mods-tgt.com`

**3. Run**

```shell
pnpm run dev
```

Frontend runs at `http://localhost:5173`.

**4. Common Errors**

For now, you may sometimes get stuck loading in certain areas of the application due to expired tokens. If you open DevTools you might see a 401 error (Unauthorised). To fix this, clear the site's local storage and refresh the page. 

---

## Health Check

```
GET /health
```

Returns database connectivity status. Use this to verify your connection string is correct after setup.
