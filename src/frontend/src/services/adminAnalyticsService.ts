import { AuthError } from "./authService";
import type { ApiError } from "./authService";

const BASE_URL = import.meta.env.VITE_API_BASE_URL;

// ── Enum mirrors ────────────────────────────────────────────────────────────

export type DashboardPeriod =
  | "Today"
  | "Last7Days"
  | "Last30Days"
  | "Last90Days"
  | "ThisMonth"
  | "ThisYear"
  | "AllTime";

// ── Response types ──────────────────────────────────────────────────────────

export interface DashboardSummary {
  totalRevenue: number;
  totalOrders: number;
  avgOrderValue: number;
  totalUsers: number;
  activeProducts: number;
}

export interface RevenueTrendPoint {
  month: string;
  revenue: number;
}

export interface OrderVolumePoint {
  month: string;
  orders: number;
}

export interface SalesByCategory {
  categoryName: string;
  orderCount: number;
  revenue: number;
  percentage: number;
}

export interface UserActivityPoint {
  week: string;
  newUsers: number;
}

export interface ProductPerformanceRow {
  rank: number;
  productName: string;
  category: string;
  sales: number;
  revenue: number;
  views: number;
  conversion: string;
}

export interface TopSellerRow {
  rank: number;
  sellerName: string;
  productCount: number;
  totalSales: number;
  revenue: number;
  rating: number;
  performance: string;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DashboardResponse {
  period: DashboardPeriod;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  summary: DashboardSummary;
  revenueTrend: RevenueTrendPoint[];
  orderVolume: OrderVolumePoint[];
  salesByCategory: SalesByCategory[];
  userActivity: UserActivityPoint[];
  productPerformance: PaginatedResult<ProductPerformanceRow>;
  topSellers: PaginatedResult<TopSellerRow>;
}

// ── Helpers ─────────────────────────────────────────────────────────────────

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let payload: ApiError = { code: "UNKNOWN", message: "Request failed." };
    try {
      payload = await res.json();
    } catch {
      // ignore parse errors
    }
    throw new AuthError(res.status, payload);
  }
  return res.json() as Promise<T>;
}

// ── API functions ───────────────────────────────────────────────────────────

/** GET /api/admin/analytics/dashboard?period=...&productPage=...&sellerPage=... */
export async function getAdminDashboard(
  period: DashboardPeriod = "Last7Days",
  productPage = 0,
  productPageSize = 10,
  sellerPage = 0,
  sellerPageSize = 10
): Promise<DashboardResponse> {
  const qs = new URLSearchParams({ period });
  if (productPage !== 0) qs.set("productPage", String(productPage));
  if (productPageSize !== 10) qs.set("productPageSize", String(productPageSize));
  if (sellerPage !== 0) qs.set("sellerPage", String(sellerPage));
  if (sellerPageSize !== 10) qs.set("sellerPageSize", String(sellerPageSize));

  const res = await fetch(`${BASE_URL}/admin/analytics/dashboard?${qs}`, {
    method: "GET",
    credentials: "include",
  });
  return handleResponse<DashboardResponse>(res);
}
