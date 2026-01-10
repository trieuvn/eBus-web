using eBusWeb.Areas.Admin.Reports;
using eBusWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using QuestPDF.Fluent;
using Supabase;

namespace eBusWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly Client _supabase;

        public DashboardController(Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<IActionResult> Index()
        {
            // ===== Load data from Supabase =====
            var bookingsRes = await _supabase.From<Booking>().Get();
            var routesRes = await _supabase.From<Models.Route>().Get();
            var passengersRes = await _supabase.From<BookingPassenger>().Get();

            var bookings = bookingsRes.Models;
            var routes = routesRes.Models;
            var passengers = passengersRes.Models;

            // ===== KPI =====
            var totalRevenue = bookings.Sum(x => x.TotalAmount);
            var totalBookings = bookings.Count;
            var activeRoutes = routes.Count;

            var pendingPayments = bookings
                .Where(x => x.BookingStatus == 0) // Pending
                .Sum(x => x.TotalAmount);

            // ===== Recent Bookings =====
            var recentBookings = bookings
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(b => new RecentBookingVM
                {
                    BookingCode = $"#BK-{b.Id}",
                    PassengerName = passengers
                        .FirstOrDefault(p => p.BookingId == b.Id)?.FullName
                        ?? b.ContactName,
                    PassengerAvatar = GetAvatar(b.ContactName),
                    Route = "N/A", // nếu chưa có Trip → Route
                    Status = MapStatus(b.BookingStatus),
                    Amount = (decimal)b.TotalAmount
                })
                .ToList();

            // ===== Route Popularity =====
            var routePopularity = routes
                .Select(r => new RoutePopularityVM
                {
                    RouteName = r.Name,
                    BookingCount = bookings.Count // tạm thời
                })
                .Take(5)
                .ToList();

            var model = new DashboardVM
            {
                TotalRevenue = (decimal)totalRevenue,
                TotalBookings = totalBookings,
                ActiveRoutes = activeRoutes,
                PendingPayments = (decimal)pendingPayments,

                RevenueGrowthPercent = 0,
                BookingGrowthPercent = 0,

                RecentBookings = recentBookings,
                RoutePopularities = routePopularity
            };

            return View(model);
        }

        // ===== Helpers =====
        private string MapStatus(int status)
        {
            return status switch
            {
                0 => "Pending",
                1 => "Confirmed",
                2 => "Cancelled",
                _ => "Unknown"
            };
        }

        private string GetAvatar(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "NA";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : name.Substring(0, 1).ToUpper();
        }
        public async Task<IActionResult> ExportPdf()
        {
            // ===== Load data from Supabase =====
            var bookings = (await _supabase.From<Booking>().Get()).Models;
            var routes = (await _supabase.From<Models.Route>().Get()).Models;

            // ===== Create PDF document =====
            var document = new DashboardReportDocument(bookings, routes);

            // ===== Generate PDF =====
            var pdfBytes = document.GeneratePdf();

            return File(pdfBytes, "application/pdf", "Dashboard_Report.pdf");
        }
    }
}
