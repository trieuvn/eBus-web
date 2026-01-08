
namespace eBusWeb.Areas.Admin.Controllers
{
    public class TripEditVM
    {
        public long Id { get; set; }
        public int RouteId { get; set; }
        public string RouteName { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string OperatorName { get; set; }
        public string BusType { get; set; }
        public int BasePrice { get; set; }
        public int Status { get; set; }
    }
}