# Dokkana — Frontend (Angular 20)

The shopper UI, store portal, and admin panel for Dokkana.

## Folder structure (foundation — fill these in)

```
src/app/
  core/                shared singletons (loaded once)
    services/          ApiService, AuthService, etc.
    guards/            auth / role route guards
    interceptors/      JWT interceptor
    models/            TypeScript interfaces matching backend DTOs
  shared/              reusable UI
    components/        buttons, cards, layout
    pipes/             formatting (currency EGP, date...)
  features/            one folder per feature area
    auth/              login, register
    products/          product list, detail, filters
    cart/              cart + checkout
    orders/            order history
    stores/            browse stores, store page
    ai-search/         natural-language search chat UI
    store-portal/      owner: products, catalog upload, business copilot
    admin/             approve stores, manage users
```

> Empty folders contain a `.gitkeep` so they survive in git. Delete it once you add real files.

## Config

API base URL lives in `src/environments/environment.ts` — update the port to match
the backend's `launchSettings.json`.

## Run

```bash
npm install
npm start        # ng serve → http://localhost:4200
```

## Notes for the team

- Bilingual (Arabic RTL + English) — plan i18n early.
- Use the JWT interceptor (core/interceptors) to attach the Bearer token to every request.
- Keep API DTO shapes in `core/models` in sync with the backend `Dokkana.BLL/DTOs`.
