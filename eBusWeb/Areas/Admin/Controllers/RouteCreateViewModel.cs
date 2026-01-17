namespace eBusWeb.Areas.Admin.Controllers
{
    public class RouteCreateViewModel
    {
        // ===== ROUTE INFO =====
        public string Name { get; set; }

        public string Origin { get; set; }

        public string Destination { get; set; }

        public int DistanceKm { get; set; }

        // Dùng để build estimated_duration (vd: 6h 30m)
        public int Hours { get; set; }

        public int Minutes { get; set; }

        // ===== STOPS =====
        public List<StopItemViewModel> Stops { get; set; } = new();
    }

    public class StopItemViewModel
    {
        public string LocationName { get; set; }

        public int StopOrder { get; set; }

        // 1: Origin | 2: Intermediate | 3: Destination
        public int StopType { get; set; }
    }
}
