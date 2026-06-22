using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ReUse.Domain.Entities;
using ReUse.Domain.Enums;
using ReUse.Infrastructure.Persistence;

namespace ReUse.Infrastructure.Seeders;

public static class OrderSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        if (await dbContext.Orders.AnyAsync())
            return;

        var products = await dbContext.Products
            .OfType<RegularProduct>()
            .Include(p => p.Owner)
            .ToListAsync();

        var users = await dbContext.Set<User>().ToListAsync();
        var userIds = users.Select(u => u.Id).ToList();
        var random = new Random(42);
        var now = DateTime.UtcNow;

        var paymentMeta = new List<(Guid productId, Guid ownerId, decimal price, DateTime createdAt, Guid buyerId)>();

        foreach (var product in products)
        {
            var buyerIds = userIds.Where(id => id != product.OwnerUserId).ToList();
            var buyerId = buyerIds[random.Next(buyerIds.Count)];
            var daysAgo = (int)(365 * Math.Pow(random.NextDouble(), 2));
            var createdAt = now.AddDays(-daysAgo);

            product.ViewCount += random.Next(50, 500);

            paymentMeta.Add((product.Id, product.OwnerUserId, product.Price, createdAt, buyerId));
        }

        var payments = paymentMeta.Select(pm => new Payment
        {
            UserId = pm.buyerId,
            Amount = pm.price,
            PaymentDate = pm.createdAt,
            PaymentMethod = random.Next(2) == 0 ? "CreditCard" : "PayPal",
            TransactionId = $"TXN-{Guid.NewGuid():N}"[..20],
            Status = PaymentStatus.Success,
            CreatedAt = pm.createdAt,
        }).ToList();

        dbContext.Set<Payment>().AddRange(payments);
        await dbContext.SaveChangesAsync();

        var orders = payments.Select((payment, i) =>
        {
            var pm = paymentMeta[i];
            return new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = pm.buyerId,
                SellerId = pm.ownerId,
                ProductId = pm.productId,
                PaymentId = payment.Id,
                Amount = pm.price,
                Currency = "USD",
                Status = OrderStatus.Delivered,
                RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks),
                CreatedAt = pm.createdAt,
                UpdatedAt = pm.createdAt,
            };
        }).ToList();

        dbContext.Orders.AddRange(orders);
        await dbContext.SaveChangesAsync();
    }
}