
namespace DomainModelPOC.Tests;

using DomainModelPOC.Domain;
using DomainModelPOC.Application;
using Xunit; 
public class OrderTests
{
    // --- Feature: field keyword (property accessor) ---

    [Fact]
    public void Order_Status_RejectsInvalidTransition()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.Status = OrderStatus.Submitted;

        // Act & Assert — field keyword enforces state machine in setter
        Assert.Throws<InvalidOperationException>(() =>
            order.Status = OrderStatus.Delivered);  // must go through Processing → Shipped first
    }

    [Fact]
    public void Order_Notes_TrimsAndNullsWhitespace()
    {
        // Demonstrates 'field' keyword: setter trims value without a backing field
        var order = CreateSampleOrder();

        order.Notes = "  Rush delivery  ";
        Assert.Equal("Rush delivery", order.Notes);

        order.Notes = "   ";
        Assert.Null(order.Notes);  // whitespace-only → null
    }

    // --- Feature: C# 14 extension properties ---

    [Fact]
    public void OrderStatus_IsTerminal_TrueForFinalStates()
    {
        // Extension method
        Assert.True(OrderStatus.Delivered.IsTerminal());
        Assert.True(OrderStatus.Cancelled.IsTerminal());
        Assert.False(OrderStatus.Processing.IsTerminal());
    }

    [Fact]
    public void OrderStatus_CanTransitionTo_FollowsStateMachine()
    {
        // Extension method in an extension block
        Assert.True(OrderStatus.Draft.CanTransitionTo(OrderStatus.Submitted));
        Assert.False(OrderStatus.Draft.CanTransitionTo(OrderStatus.Delivered));
        Assert.False(OrderStatus.Delivered.CanTransitionTo(OrderStatus.Cancelled));
    }

    // --- Feature: params ReadOnlySpan<T> (zero-alloc) ---

    [Fact]
    public void Order_AddLines_ParamsSpan_AcceptsInlineArgs()
    {
        var order = CreateSampleOrder();
        var laptop = new OrderLine(Guid.NewGuid(), "Laptop", 1, new Money(1200m));
        var mouse  = new OrderLine(Guid.NewGuid(), "Mouse",  2, new Money(25m));

        // C# 14: pass inline — no array allocation at call site
        order.AddLines(laptop, mouse);

        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(new Money(1250m), order.TotalAmount);
    }

    [Fact]
    public void Order_AddLines_RejectsNonPositiveQuantity()
    {
        var order = CreateSampleOrder();
        var badLine = new OrderLine(Guid.NewGuid(), "Widget", 0, new Money(10m));

        Assert.Throws<ArgumentException>(() => order.AddLines(badLine));
    }

    // --- Feature: Money value object with field keyword ---

    //[Fact]
    //public void Money_RejectsNegativeAmount()
    //{
    //    // field keyword in readonly record struct init
    //    Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m));
    //}

    [Fact]
    public void Money_Addition_SameCurrencySucceeds()
    {
        var a = new Money(100m);
        var b = new Money(50m);
        Assert.Equal(new Money(150m), a + b);
    }

    [Fact]
    public void Money_Addition_DifferentCurrencyThrows()
    {
        var usd = new Money(100m, "USD");
        var eur = new Money(100m, "EUR");
        Assert.Throws<InvalidOperationException>(() => _ = usd + eur);
    }

    // --- Feature: Email value object validation ---

    [Fact]
    public void Email_NormalisesToLowercase()
    {
        var email = new Email("User@Example.COM");
        Assert.Equal("user@example.com", email.Value);
    }

    //[Fact]
    //public void Email_RejectsInvalidFormat()
    //{
    //    Assert.Throws<FormatException>(() => new Email("notanemail"));
    //}

    // --- Feature: LINQ v3 — CountBy, AggregateBy, Index ---

    [Fact]
    public async Task OrderService_Summary_UsesLinqV3Operators()
    {
        var repo    = new InMemoryOrderRepository();
        var service = new OrderService(repo, TimeProvider.System);

        var alice = Customer.New("Alice", "Smith", "alice@example.com");
        var bob   = Customer.New("Bob",   "Jones", "bob@example.com");

        var line = new OrderLine(Guid.NewGuid(), "Book", 2, new Money(15m));

        var o1 = await service.CreateOrderAsync(alice, default, line);
        var o2 = await service.CreateOrderAsync(bob, default, line);
        await service.AdvanceStatusAsync(o1.Id);   // Draft → Submitted

        var summary = await service.GetSummaryAsync();

        // CountBy result
        Assert.Equal(1, summary.CountByStatus["Submitted"]);
        Assert.Equal(1, summary.CountByStatus["Draft"]);

        // AggregateBy result
        Assert.Equal(new Money(30m), summary.RevenueByCustomer["Alice Smith"]);

        // Index result — 2 recent orders
        Assert.Equal(2, summary.RecentOrders.Count);
        Assert.StartsWith("#1", summary.RecentOrders[0]);
    }

    // --- Feature: Primary constructor on service class ---

    [Fact]
    public async Task OrderService_CreateOrder_PersistsViaRepository()
    {
        // Primary constructor injection — no boilerplate ctor body
        var repo    = new InMemoryOrderRepository();
        var service = new OrderService(repo, TimeProvider.System);
        var customer = Customer.New("Jane", "Doe", "jane@example.com");

        var order = await service.CreateOrderAsync(
            customer,
            default,
            new OrderLine(Guid.NewGuid(), "Widget", 3, new Money(9.99m)));

        var loaded = await repo.GetAsync(order.Id);
        Assert.NotNull(loaded);
        Assert.Equal(order.Id, loaded!.Id);
    }

    // --- Helper ---
    static Order CreateSampleOrder()
    {
        var customer = Customer.New("Test", "User", "test@example.com");
        return Order.New(customer);
    }
}
