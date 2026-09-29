# API Reference

Complete REST API endpoint documentation for Market API.

## Base URLs

- **Development**: `http://localhost:5000`
- **HTTPS Dev**: `https://localhost:5001` or `https://localhost:7294`
- **Docker**: `http://localhost:5000`

## Authentication

Currently, the API does not require authentication. Future versions will include JWT authentication.

## Table of Contents

- [Products](#products)
- [Categories](#categories)
- [Users](#users)
- [Vendors](#vendors)
- [Orders](#orders)
- [Carts](#carts)
- [Reviews](#reviews)
- [Error Handling](#error-handling)

## Products

### Get All Products

**Endpoint**: `GET /api/products`

**Description**: Retrieve all products from the marketplace

**Response**: `200 OK`
```json
[
  {
    "id": 1,
    "name": "Laptop",
    "description": "Gaming laptop",
    "price": 1299.99,
    "discountPrice": 1099.99,
    "categoryId": 2,
    "vendorId": 1,
    "quantity": 50,
    "imageUrl": "https://example.com/laptop.jpg",
    "averageRating": 4.5,
    "reviewCount": 12
  }
]
```

**Example**:
```bash
curl http://localhost:5000/api/products
```

---

### Get Product by ID

**Endpoint**: `GET /api/products/{id}`

**Parameters**: `id` (integer, required) - Product ID

**Response**: `200 OK`
```json
{
  "id": 1,
  "name": "Laptop",
  "description": "Gaming laptop with RTX graphics",
  "price": 1299.99,
  "categoryId": 2,
  "vendorId": 1,
  "quantity": 50
}
```

**Errors**: `404 Not Found`

---

### Create Product

**Endpoint**: `POST /api/products`

**Request Body**:
```json
{
  "name": "Gaming Keyboard",
  "description": "Mechanical keyboard with RGB",
  "categoryId": 2,
  "vendorId": 1,
  "price": 149.99,
  "discountPrice": 99.99,
  "quantity": 30,
  "imageUrl": "https://example.com/keyboard.jpg",
  "sku": "KB-RGB-001"
}
```

**Response**: `201 Created`
```json
{
  "id": 12,
  "name": "Gaming Keyboard",
  "price": 149.99,
  "categoryId": 2,
  "vendorId": 1
}
```

**Validation Rules**:
- `name`: Required, max 255 characters
- `price`: Required, must be positive
- `categoryId`: Required, must reference active category
- `vendorId`: Required, must reference approved vendor
- `quantity`: Required, non-negative

**Errors**: 
- `400 Bad Request` - Validation failed
- `404 Not Found` - Category or Vendor not found
- `422 Unprocessable Entity` - Invalid data

---

### Update Product

**Endpoint**: `PUT /api/products/{id}`

**Parameters**: `id` (string, required)

**Request Body**:
```json
{
  "name": "Gaming Keyboard Pro",
  "price": 159.99,
  "quantity": 25
}
```

**Response**: `200 OK`

**Errors**: 
- `404 Not Found`
- `400 Bad Request`

---

### Delete Product

**Endpoint**: `DELETE /api/products/{id}`

**Response**: `200 OK`
```json
{
  "success": true,
  "message": "Product deleted"
}
```

**Errors**: `404 Not Found`

---

## Categories

### Get All Categories

**Endpoint**: `GET /api/categories`

**Response**: `200 OK`
```json
[
  {
    "id": "507f1f77bcf86cd799439010",
    "name": "Electronics",
    "description": "Electronic devices and accessories",
    "slugValue": "electronics",
    "isActive": true,
    "displayOrder": 1,
    "createdAt": "2026-01-01T00:00:00Z"
  }
]
```

---

### Get Category by ID

**Endpoint**: `GET /api/categories/{id}`

**Response**: `200 OK`

**Errors**: `404 Not Found`

---

### Create Category

**Endpoint**: `POST /api/categories`

**Request Body**:
```json
{
  "name": "Furniture",
  "description": "Home furniture items"
}
```

**Response**: `201 Created`

---

### Update Category

**Endpoint**: `PUT /api/categories/{id}`

**Response**: `200 OK`

---

### Delete Category

**Endpoint**: `DELETE /api/categories/{id}`

**Response**: `200 OK`

---

## Users

### Get All Users

**Endpoint**: `GET /api/users`

**Response**: `200 OK`
```json
[
  {
    "id": "507f1f77bcf86cd799439001",
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "phoneNumber": "+1234567890",
    "role": "Customer",
    "isActive": true,
    "isEmailVerified": true,
    "createdAt": "2026-01-10T08:00:00Z"
  }
]
```

---

### Get User by ID

**Endpoint**: `GET /api/users/{id}`

**Response**: `200 OK`

**Errors**: `404 Not Found`

---

### Create User

**Endpoint**: `POST /api/users`

**Request Body**:
```json
{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@example.com",
  "phoneNumber": "+1987654321"
}
```

**Response**: `201 Created`

**Validation Rules**:
- `email`: Required, valid email format
- `firstName`, `lastName`: Required
- `phoneNumber`: Valid phone format

---

## Vendors

### Get All Vendors

**Endpoint**: `GET /api/vendors`

**Response**: `200 OK`
```json
[
  {
    "id": "507f1f77bcf86cd799439002",
    "userId": "507f1f77bcf86cd799439001",
    "storeName": "Tech Store Pro",
    "storeDescription": "Premium technology products",
    "isApproved": true,
    "isActive": true,
    "averageRating": 4.8,
    "totalReviews": 150,
    "commissionRate": 0.1,
    "createdAt": "2026-01-05T12:00:00Z"
  }
]
```

---

### Create Vendor

**Endpoint**: `POST /api/vendors`

**Request Body**:
```json
{
  "userId": "507f1f77bcf86cd799439001",
  "storeName": "My Store",
  "storeDescription": "Quality products",
  "phoneNumber": "+1234567890",
  "address": "123 Main St",
  "city": "New York",
  "country": "USA",
  "zipCode": "10001"
}
```

**Response**: `201 Created`

---

### Approve Vendor

**Endpoint**: `PUT /api/vendors/{id}/approve`

**Response**: `200 OK`

**Note**: Only for admin users

---

## Orders

### Get All Orders

**Endpoint**: `GET /api/orders`

**Response**: `200 OK`
```json
[
  {
    "id": "507f1f77bcf86cd799439003",
    "customerId": "507f1f77bcf86cd799439001",
    "orderNumber": "ORD-20260115-A1B2C3D4",
    "items": [
      {
        "productId": "507f1f77bcf86cd799439011",
        "productName": "Laptop",
        "vendorId": "507f1f77bcf86cd799439002",
        "price": 1299.99,
        "quantity": 1,
        "subTotal": 1299.99
      }
    ],
    "subTotal": 1299.99,
    "shippingCost": 50.00,
    "tax": 130.00,
    "totalPrice": 1479.99,
    "status": "Pending",
    "paymentStatus": "Pending",
    "shippingAddress": "123 Main St, New York, NY 10001",
    "createdAt": "2026-01-15T14:30:00Z"
  }
]
```

---

### Create Order

**Endpoint**: `POST /api/orders`

**Request Body**:
```json
{
  "customerId": "507f1f77bcf86cd799439001",
  "items": [
    {
      "productId": "507f1f77bcf86cd799439011",
      "productName": "Laptop",
      "vendorId": "507f1f77bcf86cd799439002",
      "price": 1299.99,
      "quantity": 1
    }
  ],
  "subTotal": 1299.99,
  "shippingCost": 50.00,
  "tax": 130.00,
  "shippingAddress": "123 Main St, New York, NY 10001"
}
```

**Response**: `201 Created`

---

## Carts

### Get Cart by User ID

**Endpoint**: `GET /api/carts/user/{userId}`

**Response**: `200 OK`
```json
{
  "id": "507f1f77bcf86cd799439004",
  "userId": "507f1f77bcf86cd799439001",
  "items": [
    {
      "productId": "507f1f77bcf86cd799439011",
      "productName": "Laptop",
      "vendorId": "507f1f77bcf86cd799439002",
      "price": 1299.99,
      "quantity": 1,
      "imageUrl": "https://example.com/laptop.jpg",
      "subTotal": 1299.99
    }
  ],
  "totalPrice": 1299.99,
  "createdAt": "2026-01-15T10:00:00Z"
}
```

---

### Add to Cart

**Endpoint**: `POST /api/carts/add`

**Request Body**:
```json
{
  "userId": "507f1f77bcf86cd799439001",
  "productId": "507f1f77bcf86cd799439011",
  "quantity": 2,
  "price": 1299.99
}
```

**Response**: `200 OK`

---

### Remove from Cart

**Endpoint**: `DELETE /api/carts/{userId}/item/{productId}`

**Response**: `200 OK`

---

### Clear Cart

**Endpoint**: `DELETE /api/carts/{userId}/clear`

**Response**: `200 OK`

---

## Reviews

### Get All Reviews

**Endpoint**: `GET /api/reviews`

**Response**: `200 OK`
```json
[
  {
    "id": "507f1f77bcf86cd799439005",
    "productId": "507f1f77bcf86cd799439011",
    "vendorId": "507f1f77bcf86cd799439002",
    "customerId": "507f1f77bcf86cd799439001",
    "ratingValue": 5,
    "title": "Excellent product!",
    "comment": "Very satisfied with this purchase. Great quality and fast shipping.",
    "isVerifiedPurchase": true,
    "helpfulCount": 12,
    "imageUrls": ["https://example.com/review1.jpg"],
    "createdAt": "2026-01-14T15:30:00Z"
  }
]
```

---

### Get Reviews by Product ID

**Endpoint**: `GET /api/reviews/product/{productId}`

**Response**: `200 OK`

---

### Create Review

**Endpoint**: `POST /api/reviews`

**Request Body**:
```json
{
  "productId": "507f1f77bcf86cd799439011",
  "vendorId": "507f1f77bcf86cd799439002",
  "customerId": "507f1f77bcf86cd799439001",
  "ratingValue": 5,
  "title": "Amazing product",
  "comment": "Highly recommended!",
  "imageUrls": []
}
```

**Response**: `201 Created`

**Validation Rules**:
- `ratingValue`: Required, 1-5
- `title`: Required, max 200 characters
- `comment`: Required, max 5000 characters

---

## Error Handling

### Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["The Name field is required."],
    "Price": ["Price must be greater than 0"]
  }
}
```

### HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | OK - Request succeeded |
| 201 | Created - Resource created |
| 400 | Bad Request - Invalid data |
| 404 | Not Found - Resource not found |
| 422 | Unprocessable Entity - Validation failed |
| 500 | Internal Server Error |

---

## Testing

### With cURL

```bash
# Get all products
curl http://localhost:5000/api/products

# Create product
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","price":99.99}'

# Get by ID
curl http://localhost:5000/api/products/507f1f77bcf86cd799439011
```

### With Postman

Import the API collection from the project or use the URL:
```
http://localhost:5000/swagger
```

### Swagger/OpenAPI

Interactive API documentation available at:
```
Development: https://localhost:7294/swagger
Docker: http://localhost:5000/swagger
```

---

## Pagination (Future)

Upcoming pagination support:
```
GET /api/products?page=1&pageSize=10&sort=name&order=asc
```

