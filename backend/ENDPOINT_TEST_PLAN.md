# Dokkana API Endpoint Test Plan

Base URL:

```text
http://localhost:5243
```

Use Postman, Thunder Client, Swagger/Scalar, or any API client.

## Test Variables

Create these variables in your API client:

```text
baseUrl = http://localhost:5243
adminToken =
sellerToken =
shopperToken =
sellerStoreId =
approvedStoreId =
categoryId =
productId =
reviewId =
wishlistProductId =
orderId =
notificationId =
```

Authorization header for protected endpoints:

```text
Authorization: Bearer {{token}}
Content-Type: application/json
```

Seeded admin account:

```json
{
  "email": "admin@dokkana.com",
  "password": "Admin@123"
}
```

## 1. Health / Public Startup Checks

### 1.1 Open API root

```http
GET {{baseUrl}}/
```

Expected:

- In development, redirects to Scalar docs or returns success from the docs page.

### 1.2 Get public categories

```http
GET {{baseUrl}}/api/categories
```

Expected:

- `200 OK`
- `success = true`
- Categories list exists.

### 1.3 Get approved stores

```http
GET {{baseUrl}}/api/stores/approved
```

Expected:

- `200 OK`
- Only approved stores appear.

### 1.4 Get public products

```http
GET {{baseUrl}}/api/products?page=1&pageSize=10
```

Expected:

- `200 OK`
- Products from approved stores only.

## 2. Authentication

### 2.1 Login admin

```http
POST {{baseUrl}}/api/auth/login
```

Body:

```json
{
  "email": "admin@dokkana.com",
  "password": "Admin@123"
}
```

Expected:

- `200 OK`
- `data.role = Admin`
- Save `data.token` as `adminToken`.

### 2.2 Register shopper

Use a fresh email every test run.

```http
POST {{baseUrl}}/api/auth/register
```

Body:

```json
{
  "firstName": "Test",
  "lastName": "Shopper",
  "email": "shopper.test@example.com",
  "password": "Test@123",
  "phoneNumber": "01000000001"
}
```

Expected:

- `200 OK`
- `data.role = Shopper`
- `data.storeId = null`
- Save `data.token` as `shopperToken`.

### 2.3 Register seller / store owner

Use a fresh email every test run.

```http
POST {{baseUrl}}/api/auth/register
```

Body:

```json
{
  "firstName": "Test",
  "lastName": "Seller",
  "email": "seller.test@example.com",
  "password": "Test@123",
  "phoneNumber": "01000000002",
  "storeName": "Test Seller Store"
}
```

Expected:

- `200 OK`
- `data.role = StoreOwner`
- `data.storeId` has a value.
- Save `data.token` as `sellerToken`.
- Save `data.storeId` as `sellerStoreId`.
- Store starts pending approval.

### 2.4 Invalid login

```http
POST {{baseUrl}}/api/auth/login
```

Body:

```json
{
  "email": "missing@example.com",
  "password": "wrong"
}
```

Expected:

- `401 Unauthorized`
- `success = false`

## 3. Admin Dashboard And User Management

Use `adminToken`.

### 3.1 Admin stats

```http
GET {{baseUrl}}/api/admin/stats
```

Expected:

- `200 OK`
- Contains `usersCount`, `storesCount`, `pendingStoresCount`, `productsCount`, `ordersCount`, `revenue`.

### 3.2 Admin users list

```http
GET {{baseUrl}}/api/admin/users
```

Expected:

- `200 OK`
- Contains users with roles.

### 3.3 Shopper cannot access admin users

Use `shopperToken`.

```http
GET {{baseUrl}}/api/admin/users
```

Expected:

- `403 Forbidden`

## 4. Categories

### 4.1 Public get categories

```http
GET {{baseUrl}}/api/categories
```

Expected:

- `200 OK`

### 4.2 Admin create category

Use `adminToken`.

```http
POST {{baseUrl}}/api/categories
```

Body:

```json
{
  "name": "Test Category",
  "description": "Category for endpoint testing",
  "imageUrl": "/images/categories/test-category.jpg"
}
```

Expected:

- `201 Created`
- Save returned `data.id` as `categoryId`.

### 4.3 Get category by id

```http
GET {{baseUrl}}/api/categories/{{categoryId}}
```

Expected:

- `200 OK`

### 4.4 Admin update category

Use `adminToken`.

```http
PUT {{baseUrl}}/api/categories/{{categoryId}}
```

Body:

```json
{
  "name": "Updated Test Category",
  "description": "Updated category description",
  "imageUrl": "/images/categories/updated-test-category.jpg"
}
```

Expected:

- `200 OK`

### 4.5 Shopper cannot create category

Use `shopperToken`.

```http
POST {{baseUrl}}/api/categories
```

Body:

```json
{
  "name": "Blocked Category",
  "description": "Should fail"
}
```

Expected:

- `403 Forbidden`

## 5. Seller Store Flow

Important:

- Registering as seller with `storeName` creates the seller store automatically.
- After seller registration, use `GET /api/stores/my` to get the store.
- Use `PUT /api/stores/{id}` to update it.
- Do not call `POST /api/stores` for the same seller again unless the seller has no store.

### 5.1 Seller gets own store

Use `sellerToken`.

```http
GET {{baseUrl}}/api/stores/my
```

Expected:

- `200 OK`
- `data.id = sellerStoreId`
- `data.isApproved = false` before admin approval.

### 5.1.b Seller cannot create a second store

Use `sellerToken`.

```http
POST {{baseUrl}}/api/stores
```

Body:

```json
{
  "name": "Second Store",
  "description": "Should fail because seller already owns a store",
  "address": "Cairo, Egypt"
}
```

Expected:

- `400 Bad Request`
- Message says the seller already has a store.

### 5.2 Public approved stores should not show pending seller store

```http
GET {{baseUrl}}/api/stores/approved
```

Expected:

- `sellerStoreId` should not appear before approval.

### 5.3 Admin gets all stores

Use `adminToken`.

```http
GET {{baseUrl}}/api/stores
```

Expected:

- `200 OK`
- Pending seller store appears.

### 5.4 Seller updates own store

Use `sellerToken`.

```http
PUT {{baseUrl}}/api/stores/{{sellerStoreId}}
```

Body:

```json
{
  "name": "Updated Seller Store",
  "description": "Updated by seller",
  "logoUrl": "/images/stores/test-logo.jpg",
  "address": "Cairo, Egypt",
  "latitude": 30.0444,
  "longitude": 31.2357
}
```

Expected:

- `200 OK`

### 5.5 Admin approves seller store

Use `adminToken`.

```http
PUT {{baseUrl}}/api/stores/{{sellerStoreId}}/approve
```

Expected:

- `200 OK`
- Seller receives a `StoreApproved` notification.

### 5.6 Public approved stores now show seller store

```http
GET {{baseUrl}}/api/stores/approved
```

Expected:

- `sellerStoreId` appears.

## 6. Products

### 6.1 Seller creates product for own store

Use `sellerToken`.

```http
POST {{baseUrl}}/api/products
```

Body:

```json
{
  "title": "Test Product",
  "description": "Product created by seller",
  "price": 250,
  "count": 20,
  "imageUrl": "/images/products/test-product.jpg",
  "color": "Black",
  "size": "M",
  "brand": "Dokkana Test",
  "gender": "Unisex",
  "categoryId": {{categoryId}},
  "storeId": {{sellerStoreId}}
}
```

Expected:

- `201 Created`
- Save `data.id` as `productId`.

### 6.2 Seller updates own product

Use `sellerToken`.

```http
PUT {{baseUrl}}/api/products/{{productId}}
```

Body:

```json
{
  "title": "Updated Test Product",
  "price": 275,
  "count": 15,
  "color": "Blue"
}
```

Expected:

- `200 OK`

### 6.3 Public get product by id

```http
GET {{baseUrl}}/api/products/{{productId}}
```

Expected:

- `200 OK` if seller store is approved.
- `404 Not Found` if seller store is still pending.

### 6.4 Public product search/filter

```http
GET {{baseUrl}}/api/products?search=Test&categoryId={{categoryId}}&minPrice=100&maxPrice=500&color=Blue&page=1&pageSize=10
```

Expected:

- `200 OK`
- Products from approved stores only.

### 6.5 Get products by store

```http
GET {{baseUrl}}/api/products/store/{{sellerStoreId}}?page=1&pageSize=10
```

Expected:

- `200 OK`
- Products appear only if seller store is approved.

### 6.6 Seller cannot create product for another store

Use `sellerToken`.

```http
POST {{baseUrl}}/api/products
```

Body:

```json
{
  "title": "Blocked Product",
  "description": "Should fail",
  "price": 100,
  "count": 5,
  "categoryId": {{categoryId}},
  "storeId": 999999
}
```

Expected:

- `403 Forbidden`

### 6.7 Shopper cannot create product

Use `shopperToken`.

```http
POST {{baseUrl}}/api/products
```

Body:

```json
{
  "title": "Shopper Product",
  "price": 100,
  "count": 5,
  "categoryId": {{categoryId}},
  "storeId": {{sellerStoreId}}
}
```

Expected:

- `403 Forbidden`

## 7. Wishlist

Use `shopperToken`.

### 7.1 Add product to wishlist

```http
POST {{baseUrl}}/api/wishlist/{{productId}}
```

Expected:

- `200 OK`
- Save product id as `wishlistProductId`.

### 7.2 Get wishlist

```http
GET {{baseUrl}}/api/wishlist
```

Expected:

- `200 OK`
- Product appears.

### 7.3 Remove product from wishlist

```http
DELETE {{baseUrl}}/api/wishlist/{{wishlistProductId}}
```

Expected:

- `200 OK`

## 8. Reviews And Ratings

### 8.1 Public get product reviews

```http
GET {{baseUrl}}/api/products/{{productId}}/reviews
```

Expected:

- `200 OK`

### 8.2 Shopper creates review

Use `shopperToken`.

```http
POST {{baseUrl}}/api/products/{{productId}}/reviews
```

Body:

```json
{
  "rating": 5,
  "comment": "Great product for testing."
}
```

Expected:

- `200 OK`
- Save `data.id` as `reviewId`.

### 8.3 Shopper updates same review

Use `shopperToken`.

```http
POST {{baseUrl}}/api/products/{{productId}}/reviews
```

Body:

```json
{
  "rating": 4,
  "comment": "Updated review."
}
```

Expected:

- `200 OK`
- Same user/product review is updated.

### 8.4 Admin deletes review

Use `adminToken`.

```http
DELETE {{baseUrl}}/api/products/{{productId}}/reviews/{{reviewId}}
```

Expected:

- `200 OK`

## 9. Orders

Use `shopperToken`.

### 9.1 Place order

```http
POST {{baseUrl}}/api/orders
```

Body:

```json
{
  "shippingAddress": "Test Address, Cairo",
  "phoneNumber": "01000000001",
  "items": [
    {
      "productId": {{productId}},
      "quantity": 1
    }
  ]
}
```

Expected:

- `201 Created`
- Save `data.id` as `orderId`.
- Product stock decreases.

### 9.2 Shopper gets own orders

```http
GET {{baseUrl}}/api/orders/my
```

Expected:

- `200 OK`
- New order appears.

### 9.3 Shopper gets order by id

```http
GET {{baseUrl}}/api/orders/{{orderId}}
```

Expected:

- `200 OK`

### 9.4 Seller gets store orders

Use `sellerToken`.

```http
GET {{baseUrl}}/api/orders/store
```

Expected:

- `200 OK`
- Order appears if it contains seller's product.

### 9.5 Admin gets all orders

Use `adminToken`.

```http
GET {{baseUrl}}/api/orders
```

Expected:

- `200 OK`
- New order appears.

### 9.6 Admin updates order status

Use `adminToken`.

```http
PUT {{baseUrl}}/api/orders/{{orderId}}/status
```

Body:

```json
{
  "status": "Shipped"
}
```

Expected:

- `200 OK`
- Shopper receives an `OrderStatus` notification.

Supported statuses:

```text
Pending
Confirmed
Shipped
Delivered
Cancelled
```

## 10. Notifications

Notifications are available through REST and SignalR.

SignalR hub:

```text
{{baseUrl}}/hubs/notifications
```

SignalR event:

```text
notificationReceived
```

### 10.1 Seller gets notifications after store approval

Use `sellerToken`.

```http
GET {{baseUrl}}/api/notifications
```

Expected:

- `200 OK`
- Contains notification with `type = StoreApproved`.
- Save one notification id as `notificationId`.

### 10.2 Shopper gets unread count after order status update

Use `shopperToken`.

```http
GET {{baseUrl}}/api/notifications/unread-count
```

Expected:

- `200 OK`
- Count is greater than or equal to 1 after order status update.

### 10.3 Mark notification as read

Use the token of the notification owner.

```http
PUT {{baseUrl}}/api/notifications/{{notificationId}}/read
```

Expected:

- `200 OK`

### 10.4 Mark all notifications as read

```http
PUT {{baseUrl}}/api/notifications/read-all
```

Expected:

- `200 OK`

### 10.5 Admin sends broadcast to shoppers

Use `adminToken`.

```http
POST {{baseUrl}}/api/notifications/broadcast
```

Body:

```json
{
  "title": "Test Offer",
  "message": "Endpoint test broadcast for shoppers.",
  "targetRole": "Shopper",
  "type": "Offer",
  "link": "/products"
}
```

Expected:

- `200 OK`
- Response data is number of users notified.
- Shopper receives notification.

### 10.6 Admin sends broadcast to all users

Use `adminToken`.

```http
POST {{baseUrl}}/api/notifications/broadcast
```

Body:

```json
{
  "title": "Platform Notice",
  "message": "Broadcast to all users.",
  "type": "General"
}
```

Expected:

- `200 OK`
- All users receive notification.

## 11. Dashboards

### 11.1 Seller stats

Use `sellerToken`.

```http
GET {{baseUrl}}/api/seller/stats
```

Expected:

- `200 OK`
- Contains store id, store name, product count, orders count, units sold, revenue, low stock count.

### 11.2 Admin stats after order

Use `adminToken`.

```http
GET {{baseUrl}}/api/admin/stats
```

Expected:

- `ordersCount` increased.
- `revenue` includes the test order total.

## 12. AI Search

AI search is optional in the current backend.

### 12.1 AI search without configuration

```http
POST {{baseUrl}}/api/ai/search
```

Body:

```json
{
  "query": "black t-shirt"
}
```

Expected:

- If OpenAI/Semantic Kernel is configured: `200 OK`.
- If AI is not configured: `503 Service Unavailable` with message `AI search is not configured yet.`

## 13. Cleanup Tests

Run cleanup only after the main test flow is done.

### 13.1 Seller deletes own product

Use `sellerToken`.

```http
DELETE {{baseUrl}}/api/products/{{productId}}
```

Expected:

- `200 OK`

### 13.2 Admin deletes test category

Use `adminToken`.

```http
DELETE {{baseUrl}}/api/categories/{{categoryId}}
```

Expected:

- `200 OK`
- If category has products, delete products first.

## 14. Common Failure Checks

### Missing token

Call any protected endpoint without `Authorization`.

Expected:

- `401 Unauthorized`

### Wrong role

Call an admin endpoint using `shopperToken`.

Expected:

- `403 Forbidden`

### Invalid id

Call any id endpoint with `999999`.

Expected:

- `404 Not Found`

### Validation error

Send missing required fields.

Expected:

- `400 Bad Request`

## 15. Final Stable Checklist

Mark these before asking for push:

- [ ] Project builds with `dotnet build`.
- [ ] Database is updated with latest migrations.
- [ ] Admin login works.
- [ ] Shopper register/login works.
- [ ] Seller register/login works.
- [ ] Seller can create/update/delete own products.
- [ ] Public product list hides pending store products.
- [ ] Admin can approve store.
- [ ] Public product list shows approved store products.
- [ ] Wishlist works.
- [ ] Reviews work.
- [ ] Order placement works and decrements stock.
- [ ] Order status update works.
- [ ] Notifications work for store approval, order status, and broadcast.
- [ ] Admin dashboard works.
- [ ] Seller dashboard works.
