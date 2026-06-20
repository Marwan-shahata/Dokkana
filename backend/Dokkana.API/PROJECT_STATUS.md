# Dokkana Backend Status

## Current scope

The backend now covers the core API work for Phase 1, Phase 2 basics, and Phase 3 dashboard foundations.

AI search is intentionally optional. The API starts without an OpenAI key, and `POST /api/ai/search` returns `503` until AI configuration is added.

## Completed backend features

- JWT authentication with Identity users and roles.
- Roles: `Admin`, `StoreOwner`, `Shopper`.
- Register/login flow.
- Seller registration creates a pending store.
- Category CRUD with public reads and admin writes.
- Product listing, details, search, filters, pagination for approved stores.
- Store CRUD, approved store listing, owner store endpoint, admin approval.
- Product ownership checks for store owners.
- Wishlist endpoints.
- Reviews and ratings endpoints.
- In-app notifications with unread count, mark-read, admin broadcast, store approval, order status notifications, and SignalR real-time delivery.
- Orders with order items, stock decrement, order status tracking.
- Admin dashboard stats and user list.
- Seller dashboard stats: products, orders, revenue, units sold, low stock.
- Global exception middleware with consistent JSON responses.
- Admin seed on startup:
  - Email: `admin@dokkana.com`
  - Password: `Admin@123`

## Role permissions

### Shopper

- Register/login.
- Browse approved stores.
- Browse categories and products from approved stores.
- Search/filter products.
- View product details and reviews.
- Add/remove wishlist items.
- Create or update own product reviews.
- Place orders.
- View own order history and order details.
- View notifications.

### StoreOwner

- Register/login with a store name.
- Store starts as pending until admin approval.
- View and update own store.
- Create products only for own store, even while the store is pending approval.
- Update/delete own products.
- View seller dashboard stats.
- View orders that contain products from own store.
- Receive store approval notifications.

### Admin

- Register seeded automatically on startup.
- View all stores including pending stores.
- Approve stores.
- Delete stores.
- Manage categories.
- Delete any product.
- View all orders.
- Update order status.
- View admin dashboard stats.
- View user management list.
- Delete any review.
- Send broadcast notifications to all users or a specific role.

## Main endpoints

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/categories`
- `POST /api/categories`
- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products`
- `PUT /api/products/{id}`
- `DELETE /api/products/{id}`
- `GET /api/stores/approved`
- `GET /api/stores/my`
- `PUT /api/stores/{id}/approve`
- `GET /api/wishlist`
- `POST /api/wishlist/{productId}`
- `DELETE /api/wishlist/{productId}`
- `GET /api/products/{productId}/reviews`
- `POST /api/products/{productId}/reviews`
- `DELETE /api/products/{productId}/reviews/{reviewId}`
- `POST /api/orders`
- `GET /api/orders/my`
- `GET /api/orders/store`
- `GET /api/orders`
- `PUT /api/orders/{id}/status`
- `GET /api/admin/stats`
- `GET /api/admin/users`
- `GET /api/seller/stats`
- `GET /api/notifications`
- `GET /api/notifications/unread-count`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`
- `POST /api/notifications/broadcast`
- SignalR hub: `/hubs/notifications`

## Still needed outside this backend pass

- Angular pages/services for cart, checkout, order history, dashboards, wishlist, reviews.
- Real email provider integration for notifications. Current implementation is in-app notifications only.
- Full admin user actions such as disable user, change role, reset password.
- Inventory edit screen on frontend; backend stock exists as `Product.Count`.
- Sales charts and analytics visualization on frontend.
- AI image search and RAG ingestion once the core commerce UI is stable.
