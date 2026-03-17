using DomainModelPOC.Domain;
using DomainModelPOC.Application;

Console.WriteLine("==============================================");
Console.WriteLine("  Domain Model POC — .NET 10 + C# 14");
Console.WriteLine("==============================================\n");

// -------------------------------------------------------
// 1. Create Customers
// -------------------------------------------------------
Console.WriteLine("── [1] Creating Customers ──────────────────");
var alice = Customer.New("Alice", "Smith", "alice@example.com");
var bob   = Customer.New("Bob",   "Jones", "bob@example.com");

Console.WriteLine($"  Customer 1 : {alice.FullName}  |  {alice.Email.Value}");
Console.WriteLine($"  Customer 2 : {bob.FullName}  |  {bob.Email.Value}\n");

// -------------------------------------------------------
// 2. Create Orders with params ReadOnlySpan (C# 14)
// -------------------------------------------------------
Console.WriteLine("── [2] Creating Orders (params Span — zero alloc) ──");
var aliceOrder = Order.New(alice);
aliceOrder.AddLines(
    new OrderLine(Guid.NewGuid(), "Laptop",   1, new Money(1200m)),
    new OrderLine(Guid.NewGuid(), "Mouse",    2, new Money(25m)),
    new OrderLine(Guid.NewGuid(), "Keyboard", 1, new Money(75m))
);
aliceOrder.Notes = "  Urgent delivery  ";   // trimmed by field keyword setter

var bobOrder = Order.New(bob);
bobOrder.AddLines(
    new OrderLine(Guid.NewGuid(), "Monitor", 1, new Money(450m)),
    new OrderLine(Guid.NewGuid(), "Webcam",  1, new Money(89m))
);

Console.WriteLine($"  Alice's Order  : {aliceOrder.Id}");
Console.WriteLine($"  Lines          : {aliceOrder.Lines.Count}");
Console.WriteLine($"  Total          : {aliceOrder.TotalAmount}");
Console.WriteLine($"  Notes (trimmed): '{aliceOrder.Notes}'");
Console.WriteLine($"  Bob's Order    : {bobOrder.Id}");
Console.WriteLine($"  Total          : {bobOrder.TotalAmount}\n");

// -------------------------------------------------------
// 3. Status transitions via field keyword setter
// -------------------------------------------------------
Console.WriteLine("── [3] Status Transitions (field keyword state machine) ──");
Console.WriteLine($"  Alice before : {aliceOrder.Status.DisplayLabel()}");
aliceOrder.Status = OrderStatus.Submitted;
Console.WriteLine($"  After Submit : {aliceOrder.Status.DisplayLabel()}");
aliceOrder.Status = OrderStatus.Processing;
Console.WriteLine($"  After Process: {aliceOrder.Status.DisplayLabel()}");
aliceOrder.Status = OrderStatus.Shipped;
Console.WriteLine($"  After Ship   : {aliceOrder.Status.DisplayLabel()}");

// Demonstrate invalid transition guard
Console.WriteLine("\n  [Testing invalid transition Draft → Delivered]");
try
{
    bobOrder.Status = OrderStatus.Delivered;   // should throw
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"  Caught (expected): {ex.Message}");
}
Console.WriteLine();

// -------------------------------------------------------
// 4. Extension property IsTerminal (C# 14)
// -------------------------------------------------------
Console.WriteLine("── [4] Extension Properties (C# 14) ───────");
foreach (var status in Enum.GetValues<OrderStatus>())
{
    Console.WriteLine($"  {status.DisplayLabel(),-12} IsTerminal={status.IsTerminal()}");
}
Console.WriteLine();

// -------------------------------------------------------
// 5. Service layer + LINQ v3 (CountBy, AggregateBy, Index)
// -------------------------------------------------------
Console.WriteLine("── [5] Service Layer + LINQ v3 ─────────────");
var repo    = new InMemoryOrderRepository();
var service = new OrderService(repo, TimeProvider.System);

await repo.SaveAsync(aliceOrder);
await repo.SaveAsync(bobOrder);

// Advance Bob's order once
await service.AdvanceStatusAsync(bobOrder.Id);   // Draft → Submitted

var summary = await service.GetSummaryAsync();
Console.WriteLine($"  Total Orders : {summary.TotalOrders}");
Console.WriteLine($"  Active       : {summary.Active}");

Console.WriteLine("\n  CountBy Status:");
foreach (var (status, count) in summary.CountByStatus)
    Console.WriteLine($"    {status,-14}: {count}");

Console.WriteLine("\n  AggregateBy Revenue:");
foreach (var (name, money) in summary.RevenueByCustomer)
    Console.WriteLine($"    {name,-16}: {money}");

Console.WriteLine("\n  Index() — Recent Orders:");
foreach (var entry in summary.RecentOrders)
    Console.WriteLine($"    {entry}");

// -------------------------------------------------------
// 6. Money value object guard
// -------------------------------------------------------
Console.WriteLine("\n── [6] Value Object Guards ─────────────────");
try { _ = new Money(-50m); }
catch (ArgumentOutOfRangeException ex) { Console.WriteLine($"  Negative Money caught : {ex.ParamName}"); }

try { _ = new Email("notvalid"); }
catch (FormatException ex) { Console.WriteLine($"  Bad Email caught      : {ex.Message}"); }

Console.WriteLine("\n==============================================");
Console.WriteLine("  All methods ran successfully!");
Console.WriteLine("==============================================");
