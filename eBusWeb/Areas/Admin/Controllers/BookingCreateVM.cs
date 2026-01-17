using eBusWeb.Models;

public class BookingCreateVM
{
    public Guid UserId { get; set; }          // từ Select2

    public int? PickupStopId { get; set; }
    public int? DropoffStopId { get; set; }

    public string ContactName { get; set; }
    public string ContactMobile { get; set; }
    public string ContactEmail { get; set; }

    public double TotalAmount { get; set; }

    public List<BookingPassenger> Passengers { get; set; }
}
