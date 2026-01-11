using eBusWeb.Models;

namespace eBusWeb.Areas.Admin.Controllers
{
    public class BookingCreateVM
    {
        public Booking Booking { get; set; }
        public Payment Payment { get; set; }
        public List<BookingPassenger> Passengers { get; set; }
    }
}