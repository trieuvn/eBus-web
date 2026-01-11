namespace eBusWeb.Areas.Admin.Controllers
{
    internal class RecentBookingVM
    {
        public string BookingCode { get; set; }
        public string PassengerName { get; set; }
        public string PassengerAvatar { get; set; }
        public string Route { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
    }
}