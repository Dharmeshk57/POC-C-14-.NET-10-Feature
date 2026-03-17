namespace DomainModelPOC.Application;

using DomainModelPOC.Domain;

// -----------------------------------------------------------
// C# 14: Primary constructor in a service class
// Dependencies are concise — no constructor body noise.
// ----------------------------------------------------------
public sealed class OrderService(IOrderRepository repository, TimeProvider clock)
{
    // C# 14: Collection expressions with spread operator
    private static readonly HashSet<OrderStatus> ActiveStatuses =
        [OrderStatus.Submitted, OrderStatus.Processing, OrderStatus.Shipped];

    public async Task<Order> CreateOrderAsync(
        Customer customer,
        CancellationToken ct = default,
        params OrderLine[] lines)   // params must be last in async methods
    {
        var order = Order.New(customer);
        order.AddLines(lines);                   // forwarded to domain
        await repository.SaveAsync(order, ct);
        return order;
    }

    public async Task AdvanceStatusAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await repository.GetAsync(orderId, ct)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        var next = order.Status switch
        {
            OrderStatus.Draft      => OrderStatus.Submitted,
            OrderStatus.Submitted  => OrderStatus.Processing,
            OrderStatus.Processing => OrderStatus.Shipped,
            OrderStatus.Shipped    => OrderStatus.Delivered,
            _                      => throw new InvalidOperationException(
                                         $"Order is already in a terminal state: {order.Status.DisplayLabel()}")
        };

        order.Status = next;
        await repository.SaveAsync(order, ct);
    }

    // LINQ v3: CountBy, AggregateBy, Index
    public async Task<OrderSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var orders = await repository.GetAllAsync(ct);

        // .NET 10 LINQ: CountBy — group-count without full GroupBy overhead
        var countByStatus = orders
            .CountBy(o => o.Status)
            .ToDictionary(kv => kv.Key.DisplayLabel(), kv => kv.Value);

        // .NET 10 LINQ: AggregateBy — keyed accumulation in one pass
        var revenueByCustomer = orders
            .AggregateBy(
                o => o.Customer.FullName,
                Money.Zero,
                (acc, o) => acc + o.TotalAmount)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        // .NET 10 LINQ: Index — enumerate with index without manual counter
        var recentOrders = orders
            .OrderByDescending(o => o.PlacedAt)
            .Take(10)
            .Index()                              // returns (int Index, T Item)
            .Select(x => $"#{x.Index + 1} — {x.Item.Id} ({x.Item.Status.DisplayLabel()})")
            .ToList();

        return new OrderSummary(
            TotalOrders: orders.Count,
            Active: orders.Count(o => ActiveStatuses.Contains(o.Status)),
            CountByStatus: countByStatus,
            RevenueByCustomer: revenueByCustomer,
            RecentOrders: recentOrders);
    }
}

public sealed record OrderSummary(
    int TotalOrders,
    int Active,
    Dictionary<string, int> CountByStatus,
    Dictionary<string, Money> RevenueByCustomer,
    List<string> RecentOrders);

// -----------------------------------------------------------
// Repository interface (domain boundary)
// -----------------------------------------------------------
public interface IOrderRepository
{
    Task<Order?> GetAsync(Guid id, CancellationToken ct = default);
    Task<List<Order>> GetAllAsync(CancellationToken ct = default);
    Task SaveAsync(Order order, CancellationToken ct = default);
}

// -----------------------------------------------------------
// In-memory implementation for the POC
// -----------------------------------------------------------
public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _store = [];

    public Task<Order?> GetAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public Task<List<Order>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(_store.Values.ToList());

    public Task SaveAsync(Order order, CancellationToken ct = default)
    {
        _store[order.Id] = order;
        return Task.CompletedTask;
    }
}
