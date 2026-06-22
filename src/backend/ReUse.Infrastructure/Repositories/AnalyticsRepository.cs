using System.Globalization;

using Microsoft.EntityFrameworkCore;

using ReUse.Application.DTOs.Analytics;
using ReUse.Application.Interfaces.Repository;
using ReUse.Domain.Entities;
using ReUse.Domain.Enums;
using ReUse.Infrastructure.Persistence;

namespace ReUse.Infrastructure.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly ApplicationDbContext _context;

    public AnalyticsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(DateTime from, DateTime to)
    {
        var totalRevenue = await _context.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Success && p.PaymentDate >= from && p.PaymentDate <= to)
            .SumAsync(p => p.Amount);

        var totalOrders = await _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.CreatedAt >= from && o.CreatedAt <= to);

        var totalUsers = await _context.Users
         .AsNoTracking()
         .CountAsync(u => u.CreatedAt >= from && u.CreatedAt <= to);

        var activeProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.Status == ProductStatus.Active);

        return new DashboardSummaryDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            AvgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0,
            TotalUsers = totalUsers,
            ActiveProducts = activeProducts,
        };
    }

    public async Task<List<RevenueTrendDto>> GetRevenueTrendAsync(DateTime from, DateTime to)
    {
        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Success && p.PaymentDate >= from && p.PaymentDate <= to)
            .ToListAsync();

        return payments
            .GroupBy(p => $"{p.PaymentDate.Year}-{p.PaymentDate.Month:D2}")
            .Select(g => new RevenueTrendDto
            {
                Month = g.Key,
                Revenue = g.Sum(p => p.Amount),
            })
            .OrderBy(r => r.Month)
            .ToList();
    }

    public async Task<List<OrderVolumeDto>> GetOrderVolumeAsync(DateTime from, DateTime to)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .ToListAsync();

        return orders
            .GroupBy(o => $"{o.CreatedAt.Year}-{o.CreatedAt.Month:D2}")
            .Select(g => new OrderVolumeDto
            {
                Month = g.Key,
                Orders = g.Count(),
            })
            .OrderBy(o => o.Month)
            .ToList();
    }

    public async Task<List<SalesByCategoryDto>> GetSalesByCategoryAsync(DateTime from, DateTime to)
    {
        var data = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Product)
                .ThenInclude(p => p.Category)
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .ToListAsync();

        var totalRevenue = data.Sum(o => o.Amount);

        var grouped = data
            .GroupBy(o => o.Product.Category.Name)
            .Select(g => new SalesByCategoryDto
            {
                CategoryName = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.Amount),
                Percentage = totalRevenue > 0 ? Math.Round((double)(g.Sum(o => o.Amount) / totalRevenue) * 100, 1) : 0,
            })
            .OrderByDescending(s => s.Revenue)
            .ToList();

        return grouped;
    }

    public async Task<List<UserActivityDto>> GetUserActivityAsync(DateTime from, DateTime to)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => u.CreatedAt >= from && u.CreatedAt <= to)
            .ToListAsync();

        return users
            .GroupBy(u => GetWeekKey(u.CreatedAt))
            .Select(g => new UserActivityDto
            {
                Week = g.Key,
                NewUsers = g.Count(),
            })
            .OrderBy(u => u.Week)
            .ToList();
    }

    public async Task<List<ProductPerformanceRow>> GetProductPerformanceAsync(DateTime from, DateTime to)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.CreatedAt <= to)
            .ToListAsync();

        var orderData = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .GroupBy(o => o.ProductId)
            .Select(g => new { ProductId = g.Key, Sales = g.Count(), Revenue = g.Sum(o => o.Amount) })
            .ToListAsync();

        var orderLookup = orderData.ToDictionary(o => o.ProductId);

        var items = products
            .Select(p =>
            {
                var stats = orderLookup.GetValueOrDefault(p.Id);
                var sales = stats?.Sales ?? 0;
                var revenue = stats?.Revenue ?? 0;
                var conversion = p.ViewCount > 0 ? (double)sales / p.ViewCount * 100 : 0;

                return new ProductPerformanceRow
                {
                    ProductName = p.Title,
                    Category = p.Category.Name,
                    Sales = sales,
                    Revenue = revenue,
                    Views = p.ViewCount,
                    Conversion = $"{conversion:F1}%",
                };
            })
            .OrderByDescending(p => p.Revenue)
            .Select((p, i) => p with { Rank = i + 1 })
            .ToList();

        return items;
    }

    public async Task<List<TopSellerRow>> GetTopSellersAsync(DateTime from, DateTime to)
    {
        var sellerIds = await _context.Orders
        .AsNoTracking()
        .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
        .Select(o => o.SellerId)
        .Distinct()
        .ToListAsync();

        var sellers = await _context.Users
            .AsNoTracking()
            .Where(u => sellerIds.Contains(u.Id))
            .ToListAsync();

        var productCounts = await _context.Products
            .AsNoTracking()
            .GroupBy(p => p.OwnerUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        var productCountLookup = productCounts.ToDictionary(p => p.UserId, p => p.Count);

        var revenueBySeller = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Product)
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .GroupBy(o => o.Product.OwnerUserId)
            .Select(g => new { SellerId = g.Key, Revenue = g.Sum(o => o.Amount), Sales = g.Count() })
            .ToListAsync();

        var revenueLookup = revenueBySeller.ToDictionary(r => r.SellerId);

        var items = sellers
            .Select(s =>
            {
                var stats = revenueLookup.GetValueOrDefault(s.Id);
                var revenue = stats?.Revenue ?? 0;
                var sales = stats?.Sales ?? 0;
                var performance = s.RatingsAverage >= 4.5m ? "Excellent"
                    : s.RatingsAverage >= 4.0m ? "Good"
                    : s.RatingsAverage >= 3.0m ? "Average"
                    : "Needs Improvement";

                return new TopSellerRow
                {
                    SellerName = s.FullName,
                    ProductCount = productCountLookup.GetValueOrDefault(s.Id),
                    TotalSales = sales,
                    Revenue = revenue,
                    Rating = (double)s.RatingsAverage,
                    Performance = performance,
                };
            })
            .OrderByDescending(s => s.Revenue)
            .Select((s, i) => s with { Rank = i + 1 })
            .ToList();

        return items;
    }

    private static string GetWeekKey(DateTime date)
    {
        var year = ISOWeek.GetYear(date);
        var week = ISOWeek.GetWeekOfYear(date);
        return $"{year}-W{week:D2}";
    }
}