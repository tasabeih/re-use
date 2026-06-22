using ReUse.Application.DTOs.Analytics;
using ReUse.Application.Enums;

namespace ReUse.Application.Interfaces.Services;

public interface IAnalyticsService
{
    Task<DashboardResponse> GetDashboardAsync(DashboardPeriod period, int productPage = 0, int productPageSize = 10, int sellerPage = 0, int sellerPageSize = 10);
}