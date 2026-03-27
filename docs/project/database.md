# Database Schema

The application uses **SQL Server** with **Entity Framework Core** and **ASP.NET Core Identity**. All primary keys are `GUID` (`uniqueidentifier`). Monetary values use `decimal(9,2)`.

---

## Tables

### Products

Stores the product catalog.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `Id` | `uniqueidentifier` | NO | PK | |
| `Name` | `nvarchar(200)` | NO | | Max 200 chars |
| `Description` | `nvarchar(2000)` | NO | | Max 2000 chars |
| `Stock` | `bit` | NO | | `true` = in stock |
| `Price` | `decimal(9,2)` | NO | | Up to 9,999,999.99 |
| `CreatedAt` | `datetime2` | NO | | UTC timestamp |

### Orders

Represents a customer order.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `Id` | `uniqueidentifier` | NO | PK | |
| `CustomerId` | `uniqueidentifier` | NO | FK → `AspNetUsers.Id` | OnDelete: Restrict |
| `Description` | `nvarchar(2000)` | NO | | Max 2000 chars |
| `Status` | `nvarchar(max)` | NO | | Stored as string enum |
| `TotalAmount` | `decimal(9,2)` | NO | | Sum of items at order time |
| `CreatedAt` | `datetime2` | NO | | UTC timestamp |
| `UpdatedAt` | `datetime2` | YES | | Set on status change |

**Valid `Status` values:** `Pending`, `Processing`, `Shipped`, `Delivered`, `Cancelled`

### OrderItems

Represents a single product line within an order.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `Id` | `uniqueidentifier` | NO | PK | |
| `OrderId` | `uniqueidentifier` | NO | FK → `Orders.Id` | OnDelete: Cascade |
| `ProductId` | `uniqueidentifier` | NO | FK → `Products.Id` | OnDelete: Restrict |
| `Quantity` | `int` | NO | | Min 1, Max 999 |
| `UnitPrice` | `decimal(9,2)` | NO | | Price snapshot at order time |

### AspNetUsers (Identity)

Managed by ASP.NET Core Identity. Extended with a `CreatedAt` column.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | NO | PK |
| `UserName` | `nvarchar(256)` | YES | Unique index |
| `NormalizedUserName` | `nvarchar(256)` | YES | Unique index |
| `Email` | `nvarchar(256)` | YES | |
| `NormalizedEmail` | `nvarchar(256)` | YES | Index |
| `EmailConfirmed` | `bit` | NO | |
| `PasswordHash` | `nvarchar(max)` | YES | PBKDF2 hash |
| `SecurityStamp` | `nvarchar(max)` | YES | |
| `ConcurrencyStamp` | `nvarchar(max)` | YES | |
| `PhoneNumber` | `nvarchar(max)` | YES | |
| `PhoneNumberConfirmed` | `bit` | NO | |
| `TwoFactorEnabled` | `bit` | NO | |
| `LockoutEnd` | `datetimeoffset` | YES | |
| `LockoutEnabled` | `bit` | NO | |
| `AccessFailedCount` | `int` | NO | |
| `CreatedAt` | `datetime2` | NO | Custom — set on registration |

### AspNetRoles (Identity)

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | NO | PK |
| `Name` | `nvarchar(256)` | YES | Unique index |
| `NormalizedName` | `nvarchar(256)` | YES | Unique index |
| `ConcurrencyStamp` | `nvarchar(max)` | YES | |

Seeded values: `admin`, `user`

### AspNetUserRoles (Identity)

Junction table for the user ↔ role relationship.

| Column | Type | Nullable | Constraints |
|---|---|---|---|
| `UserId` | `uniqueidentifier` | NO | PK, FK → `AspNetUsers.Id` |
| `RoleId` | `uniqueidentifier` | NO | PK, FK → `AspNetRoles.Id` |

### Other Identity Tables

| Table | Purpose |
|---|---|
| `AspNetUserClaims` | Claims attached to individual users |
| `AspNetRoleClaims` | Claims attached to roles |
| `AspNetUserLogins` | External login providers (e.g. Google OAuth) |
| `AspNetUserTokens` | Tokens for password reset and 2FA |

---

## Relationships

```
AspNetUsers ──────────────────────────────────────────────┐
     │                                                     │
     │ 1                                                   │ (many)
     ▼                                                     │
  Orders (CustomerId → AspNetUsers.Id, Restrict)          │
     │                                                     │
     │ 1                                              AspNetUserRoles
     ▼                                                     │
  OrderItems (OrderId → Orders.Id, Cascade)               │ (many)
     │                                                     │
     │ (many)                                         AspNetRoles
     ▼
  Products (ProductId → Products.Id, Restrict)
```

| Relationship | Type | FK | On Delete |
|---|---|---|---|
| `Order` → `AspNetUsers` | Many-to-One | `Orders.CustomerId` | **Restrict** — cannot delete a user who has orders |
| `OrderItem` → `Order` | Many-to-One | `OrderItems.OrderId` | **Cascade** — deleting an order deletes its items |
| `OrderItem` → `Product` | Many-to-One | `OrderItems.ProductId` | **Restrict** — cannot delete a product referenced by order items |
| `AspNetUserRoles` → `AspNetUsers` / `AspNetRoles` | Many-to-Many | Composite PK | Cascade (Identity default) |

---

## Design Decisions

| Decision | Reason |
|---|---|
| GUID primary keys | Distributed-friendly; avoids sequential ID guessing |
| `decimal(9,2)` for money | Supports values up to 9,999,999.99; avoids floating-point errors |
| `OrderStatus` stored as string | Human-readable in the database; resilient to enum reordering |
| `UnitPrice` snapshot in `OrderItems` | Preserves the price at the time of purchase; product price changes do not affect historical orders |
| No navigation property `Order → User` | Avoids a dependency from Domain on Infrastructure (`ApplicationUser` lives in Infrastructure) |
| Restrict on product/user delete | Prevents orphaned order data |
| Cascade on order delete | Ensures `OrderItems` are cleaned up when an order is removed |
