using eBusWeb.Models;

namespace eBusWeb.Areas.Admin.Controllers
{
    public class RouteWithStopsDto
    {
        // ===== ROUTE =====
        public int Id { get; set; }

        public string Name { get; set; }

        public string Origin { get; set; }

        public string Destination { get; set; }

        public int DistanceKm { get; set; }

        // DB: estimated_duration (text)
        public string EstimatedDuration { get; set; }

        // ===== STOPS =====
        public List<RouteStop> Stops { get; set; } = new();
    }
}
