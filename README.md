# Domain Model POC — C# 14 / .NET 10 Feature Showcase

A proof-of-concept **Domain-Driven Design** solution demonstrating modern C# 14 and .NET 10 language features in a real-world order management domain model.

## 🎯 Overview

This POC showcases how the latest C# and .NET capabilities can be applied to write clean, efficient, and expressive domain models. It implements a simplified order processing system with customers, orders, and line items while highlighting cutting-edge language features.

## ✨ Features Demonstrated

### **C# 14 Language Features**

#### 1. **`field` Keyword (Property Accessor)**
Eliminates backing fields for computed properties with validation logic:
- ✅ `Order.Status` — enforces state machine transitions
- ✅ `Order.Notes` — trims whitespace and normalizes to null
- ✅ `Money.Amount` — validates non-negative amounts
- ✅ `Email.Value` — validates and normalizes email addresses
- ✅ `Customer.FirstName` / `LastName` — trims and validates on init

#### 2. **Extension Properties (Extension Blocks)**
Adds computed properties to enums without polluting the type:
- ✅ `OrderStatus.IsTerminal()` — identifies final states (Delivered, Cancelled)
- ✅ `OrderStatus.CanTransitionTo(OrderStatus)` — state machine rules
- ✅ `OrderStatus.DisplayLabel()` — human-readable status names

#### 3. **`params ReadOnlySpan<T>`**
Zero-allocation variadic methods for performance:
- ✅ `Order.AddLines(params ReadOnlySpan<OrderLine>)` — add multiple lines inline without array allocation

#### 4. **Primary Constructors**
Concise dependency injection and object initialization:
- ✅ `Customer` — inline parameter validation
- ✅ `Order` — immutable core properties
- ✅ `OrderService` — DI without boilerplate constructor bodies

### **.NET 10 LINQ v3 Operators**

#### 5. **CountBy**
Group and count in one pass without full `GroupBy` overhead:

#### 6. **AggregateBy**
Keyed accumulation (like group + aggregate) in a single operation:


#### 7. **Index**
Enumerate with index without manual counters:

### **Domain-Driven Design Patterns**

- ✅ **Entities** — `Order`, `Customer` with unique identifiers
- ✅ **Value Objects** — `Money`, `Email`, `OrderLine` (immutable records/structs)
- ✅ **Aggregates** — `Order` as aggregate root managing `OrderLine` collection
- ✅ **Repository Pattern** — `IOrderRepository` with in-memory implementation
- ✅ **Domain Services** — `OrderService` for orchestration
- ✅ **State Machine** — Order status transitions with validation

## 🏗️ Project Structure

````````markdown
- `src/` - Solution directory
  - `OrderManagement/` - Order management domain
    - `Entities/` - Domain entities (`Order`, `Customer`)
    - `ValueObjects/` - Value objects (`Money`, `Email`, `OrderLine`)
    - `Aggregates/` - Aggregate roots (e.g., `Order`)
    - `Repositories/` - Repository interfaces and implementations
    - `Services/` - Domain services (e.g., `OrderService`)
    - `Specifications/` - LINQ specifications for queries
  - `OrderManagement.sln` - Solution file
````````

## 🧪 Test Coverage

All features are validated with unit tests in `OrderTests.cs`:

| Feature | Test Method |
|---------|-------------|
| State machine validation | `Order_Status_RejectsInvalidTransition()` |
| `field` keyword trimming | `Order_Notes_TrimsAndNullsWhitespace()` |
| Extension properties | `OrderStatus_IsTerminal_TrueForFinalStates()` |
| State transitions | `OrderStatus_CanTransitionTo_FollowsStateMachine()` |
| `params ReadOnlySpan<T>` | `Order_AddLines_ParamsSpan_AcceptsInlineArgs()` |
| Money arithmetic | `Money_Addition_SameCurrencySucceeds()` |
| Currency validation | `Money_Addition_DifferentCurrencyThrows()` |
| Email normalization | `Email_NormalisesToLowercase()` |
| **LINQ v3: CountBy** | `OrderService_Summary_UsesLinqV3Operators()` |
| **LINQ v3: AggregateBy** | `OrderService_Summary_UsesLinqV3Operators()` |
| **LINQ v3: Index** | `OrderService_Summary_UsesLinqV3Operators()` |
| Primary constructors | `OrderService_CreateOrder_PersistsViaRepository()` |

## 🚀 Running the POC

### Prerequisites
- .NET 10 SDK (or later)
- Visual Studio 2026 Preview or later

### Run Tests

### Build Solution

## 💡 Key Takeaways

1. **`field` keyword** drastically reduces boilerplate for validated properties
2. **Extension properties** keep enum types clean while adding behavior
3. **`params ReadOnlySpan<T>`** eliminates allocations for small call-sites
4. **Primary constructors** make DI and immutability more concise
5. **LINQ v3 operators** (`CountBy`, `AggregateBy`, `Index`) simplify common aggregation patterns

## 📚 Domain Model Highlights

### Order State Machine
Enforced via `OrderStatus` extension properties and `Order.Status` setter validation.

### Value Objects
- **`Money`** — Currency-aware, validates non-negative amounts
- **`Email`** — Auto-normalizes to lowercase, validates format
- **`OrderLine`** — Immutable record with quantity/price

### Aggregate Boundaries
- `Order` owns its collection of `OrderLine` objects
- External code cannot modify lines directly (encapsulation)

## 📝 License

This is a proof-of-concept for educational purposes.

---

**Target Framework**: .NET 10  
**Language Version**: C# 14  
**Test Framework**: xUnit