using eBusWeb.Models;

namespace eBusWeb.Areas.Admin.Controllers
{
    public class BookingEditVM
    {
        public Booking Booking { get; set; }
        public User User { get; set; }
        public Trip Trip { get; set; }
        public List<BookingPassenger> Passengers { get; set; }

        public Payment? Payment { get; set; }

        public List<RouteStop> AvailablePickupStops { get; set; } = new();
        public List<RouteStop> AvailableDropoffStops { get; set; } = new();
        public Models.Route Route { get; set; }
    }

}