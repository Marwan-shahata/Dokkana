# Dokkana Role Scenarios

## Register Scenario

Current backend behavior:

- The register endpoint is `POST /api/auth/register`.
- The request does not send a `role` field.
- If `StoreName` is empty or missing, the new account becomes a `Shopper`.
- If `StoreName` is provided, the new account becomes a `StoreOwner` and a store is created automatically with `IsApproved = false`.

Recommended frontend flow:

1. User fills normal account data:
   - First name
   - Last name
   - Email
   - Password
   - Phone number
2. UI asks whether the user wants to buy only or sell products.
3. If the user chooses shopper, do not show/send `StoreName`.
4. If the user chooses seller, show `StoreName` and send it with the register request.
5. Backend returns JWT plus the assigned role:
   - `Shopper`
   - `StoreOwner`

Important note: If the agreed UI text says `Seller`, the backend role name currently is `StoreOwner`. The meaning is the same: seller/store owner.

## Shopper Scenario

A shopper is the normal customer.

Shopper can:

- Register and login.
- Browse public categories.
- Browse products from approved stores.
- Search and filter products from approved stores.
- Open product details.
- Browse approved stores.
- Add products to wishlist.
- Remove products from wishlist.
- Add reviews and ratings to products.
- Update their own review for the same product.
- Place orders.
- View their own order history.
- View their own order details.
- View notifications.

Shopper cannot:

- Create, update, or delete products.
- Create or approve stores.
- Access seller dashboard.
- Access admin dashboard.
- Update order status.

## StoreOwner / Seller Scenario

A seller is called `StoreOwner` in the backend.

Seller registration:

- Seller registers with `StoreName`.
- Backend creates a store linked to the seller.
- Store starts as pending approval: `IsApproved = false`.
- Admin later approves the store.

Seller can:

- Login.
- View their own store.
- Update their own store info.
- Create products for their own store.
- Update products that belong to their own store.
- Delete products that belong to their own store.
- View seller dashboard statistics.
- View orders that contain products from their store.
- Manage stock through the product `Count` field.
- View notifications, including store approval notifications.

Seller cannot:

- Create products for another seller's store.
- Update or delete products from another seller's store.
- Approve their own store.
- View all users.
- View all platform orders.
- Update order status.
- Access admin dashboard.

Approval behavior:

- Seller can manage their own products even if the store is still pending.
- Public product visibility depends on the store approval flow.
- Admin approval makes the store visible through approved store endpoints.

## Admin Scenario

Admin is the platform owner role.

Seeded admin account:

- Email: `admin@dokkana.com`
- Password: `Admin@123`

Admin can:

- Login.
- View all stores, including pending stores.
- Approve seller stores.
- Delete stores.
- Manage categories.
- Delete any product.
- View all orders.
- Update order status.
- View admin dashboard statistics.
- View user list.
- Delete any review.
- Send broadcast notifications to all users or a specific role.

Admin cannot / should not:

- Place shopper orders as admin.
- Own seller product inventory unless a separate seller account is used.

## Current Role Names

- Customer/user: `Shopper`
- Seller: `StoreOwner`
- Admin: `Admin`

If the frontend uses labels like `User`, `Client`, or `Seller`, map them like this:

- `User` / `Client` / `Customer` => `Shopper`
- `Seller` => `StoreOwner`
- `Admin` => `Admin`

## Main Endpoints By Role

Public:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/categories`
- `GET /api/products`
- `GET /api/products/{id}`
- `GET /api/stores/approved`
- `GET /api/products/{productId}/reviews`
- `GET /api/notifications`
- `GET /api/notifications/unread-count`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`

Shopper:

- `GET /api/wishlist`
- `POST /api/wishlist/{productId}`
- `DELETE /api/wishlist/{productId}`
- `POST /api/products/{productId}/reviews`
- `POST /api/orders`
- `GET /api/orders/my`

Seller / StoreOwner:

- `GET /api/stores/my`
- `POST /api/stores`
- `PUT /api/stores/{id}`
- `POST /api/products`
- `PUT /api/products/{id}`
- `DELETE /api/products/{id}`
- `GET /api/orders/store`
- `GET /api/seller/stats`

Admin:

- `GET /api/stores`
- `PUT /api/stores/{id}/approve`
- `DELETE /api/stores/{id}`
- `POST /api/categories`
- `PUT /api/categories/{id}`
- `DELETE /api/categories/{id}`
- `GET /api/orders`
- `PUT /api/orders/{id}/status`
- `GET /api/admin/stats`
- `GET /api/admin/users`
- `POST /api/notifications/broadcast`

## Real-Time Notifications

Notifications are stored in the database and also pushed in real time with SignalR.

Hub URL:

```text
{{baseUrl}}/hubs/notifications
```

Client event:

```text
notificationReceived
```

JavaScript/Angular client example:

```ts
const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${apiBaseUrl}/hubs/notifications`, {
    accessTokenFactory: () => token
  })
  .withAutomaticReconnect()
  .build();

connection.on('notificationReceived', notification => {
  console.log('New notification', notification);
});

await connection.start();
```

Groups are handled automatically on connect:

- `user:{userId}`
- `role:Admin`
- `role:StoreOwner`
- `role:Shopper`
