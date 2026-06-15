# What I changed in Dokkana

Short version: I took all of our branches, merged them into one project that
actually builds and runs, fixed the things that were broken, and then connected
the Angular frontend to the .NET backend so the app works end to end (login,
browsing, cart, checkout, merchant portal, admin). This file lists what I
touched so everyone knows what is new.

## 1. Merged everything into one working project

- Our work was spread across 8 branches. After comparing them, `main` (which was
  the same as `esraa-frontend`) already had the newest version of almost
  everything: auth, admin dashboard, products, categories, stores, and the
  frontend. The `fatma-*`, `Sprint1_Sprint2`, `AppDesignV01`, and
  `Auth-features` branches had no unique files left; they were older states that
  were already merged.
- The only branch with work that was not merged yet was Marwan's (`0_marwan`):
  the real AI search (Semantic Kernel + OpenAI, the product-search plugin and its
  DTOs). I brought that in on top of `main`, because `main` only had an empty stub
  for it.
- Result: one `Dokkana/` folder where both the backend and the frontend build and
  run.

## 2. Fixed what was broken

Frontend issues that stopped it from running:

- `app.ts` still had an unresolved merge conflict (two component blocks and a
  duplicate class). I rebuilt it as a clean shell: navbar, router outlet, footer.
- The admin dashboard was being rendered on every page (leftover from testing). I
  moved it to the `/admin` route only.
- `main.ts` was overriding the app config and dropping its providers, so
  `HttpClient` was never registered. I moved everything into `app.config.ts` with
  the router and HttpClient providers.
- The API URL was pointing at the wrong port. I set it to the port the backend
  actually serves, and told the backend to skip the HTTPS redirect in development
  so the dev server connects without a certificate prompt.

Backend issues:

- The AI agent only worked when an OpenAI key was set, but the placeholder key in
  `appsettings` made every AI call throw. Now only a real key turns on the live
  agent; otherwise it returns a clear "AI not configured" message instead of
  crashing.
- There was no database migration step at startup, so a fresh database crashed on
  boot or on the first cart/order call. I added an automatic migrate-on-startup.
- I fixed the dev port so the API and the frontend/CORS settings line up.

## 3. Connected the frontend to the backend (the big one)

Auth:

- A real `AuthService` with JWT storage, an HTTP interceptor that attaches the
  token, an auth guard and a role guard, logout, and redirects based on role.
- Login calls the backend login; sign-up calls register (and creates a store
  owner when a store name is given). Backend validation errors now show in the
  form instead of going to the console.
- `/profile` and `/cart` require login, `/admin` is admin only, and `/merchant`
  is store-owner only.

Shopper side:

- A service layer for products, categories, stores, and AI search.
- Real stores list, store detail (store plus its products), and product detail
  pages, all with loading/empty/error states.
- The landing page loads real products and category tabs from the API, with a
  demo fallback so the page never looks broken.
- The search box runs a real backend search.
- The cart is connected to a real backend cart (add/update/remove/clear),
  checkout creates a reservation and shows a pickup code, and the navbar cart
  badge syncs from the backend.
- The profile shows the signed-in user, real order history, wishlist, saved
  stores, and review count.

Merchant portal (`/merchant`):

- Store owners get their own protected dashboard. They can create and edit their
  store profile, manage their products (create/edit/delete), see inventory stats
  (count, value, low stock, out of stock), and upload real product/store images.
- Catalog upload and Business Copilot are left as honest placeholders (see "not
  done yet" below).

Admin dashboard (`/admin`):

- It now uses real data instead of mock data: store list with approve/delete,
  full category management, users and orders, stats computed from real data, and
  an audit log section.

## 4. New backend features

- Server-side ownership checks: a store owner can only edit or delete their own
  store and products; other users get 403, and missing items get 404. Product
  create binds to the caller's own store instead of trusting the request body.
- Cart, order, and reservation backend: models, a migration, and APIs for the
  cart, checkout, pickup codes, stock reduction, and order history. Orders
  snapshot the product title/image/price so history stays readable even after a
  product is later edited.
- Wishlist, saved/followed stores, product reviews, audit logging, and extra user
  profile fields (address, gender, location, avatar, join date), plus an image
  upload endpoint.
- AI search now returns ranked product cards plus the answer text, not just text.
  A keyword/price matcher ranks products so results are useful even without an
  OpenAI key; the AI only enriches the answer text. I also added a rate limit and
  a basic prompt guardrail on that endpoint.
- Migrations: InitialCreate, AddCartAndOrders, AddMarketplaceEngagement. On first
  run the app seeds the roles, categories, an admin, a sample store owner, and 24
  demo products.

## 5. Design, polish, and performance

- Wrote down the "Souk Modern" design direction (colors, type, spacing, radius,
  motion) so the screens stay consistent.
- Added a global focus ring, a skip-to-content link, and reduced-motion support.
- Lazy-loaded the heavy routes (admin, profile, cart, merchant) to shrink the
  initial bundle.
- Replaced the Unsplash demo image URLs with local images, and removed the Google
  Fonts dependency (now local system fonts) so the build and the page do not
  depend on fetching anything online.
- Fixed the mobile navbar overflow.

## 6. Not done yet (on purpose)

So nobody goes looking for these, they are deliberately out of scope for now:

- Catalog PDF upload and Q&A (RAG / vector search).
- Business Copilot (sales/stock chat for store owners).
- Image/photo search, voice search, and "similar products".

The merchant portal shows placeholders for the catalog and copilot tabs so the UI
is honest about what is not built yet.

## How to run it

Backend (terminal 1):

```bash
cd backend/Dokkana.API
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."   # optional, turns on real AI answers
dotnet run                                          # http://localhost:5034
```

Frontend (terminal 2):

```bash
cd frontend
npm install
npm start                                            # http://localhost:4200
```

Test logins: admin `admin@dokkana.com` / `Admin123!`, store owner
`store@dokkana.com` / `Store123!`.
