namespace eBusWeb.Areas.Admin.Controllers
{
    public class RouteCreateViewModel
    {
        public string Name { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        // Danh sách các trạm dừng gửi từ View
        public List<StopItemViewModel> Stops { get; set; }
    }

    public class StopItemViewModel
    {
        public string LocationName { get; set; }
        public string Time { get; set; }
        public int StopOrder { get; set; }
        public int StopType { get; set; } // 1: Origin, 2: Intermediate, 3: Destination
    }
}