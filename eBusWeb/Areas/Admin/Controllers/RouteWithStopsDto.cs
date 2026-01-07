using eBusWeb.Models;

namespace eBusWeb.Areas.Admin.Controllers
{
    public class RouteWithStopsDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int EstDuration { get; set; }
        public List<RouteStop> Stops { get; set; }
    }

}