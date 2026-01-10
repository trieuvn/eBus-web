
namespace eBusWeb.Areas.Admin.Controllers
{
    internal class DashboardVM
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int ActiveRoutes { get; set; }
        public decimal PendingPayments { get; set; }
        public int RevenueGrowthPercent { get; set; }
        public int BookingGrowthPercent { get; set; }
        public List<RecentBookingVM> RecentBookings { get; set; }
        public List<RoutePopularityVM> RoutePopularities { get; set; }
    }
}