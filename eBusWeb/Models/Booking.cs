using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace eBusWeb.Models
{
    [Table("Bookings")]
    public class Booking : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("trip_id")]
        public long TripId { get; set; }

        [Column("pickup_stop_id")]
        public int? PickupStopId { get; set; }

        [Column("dropoff_stop_id")]
        public int? DropoffStopId { get; set; }

        [Column("contact_name")]
        public string ContactName { get; set; }

        [Column("contact_mobile")]
        public string ContactMobile { get; set; }

        [Column("contact_email")]
        public string ContactEmail { get; set; }

        [Column("total_amount")]
        public double TotalAmount { get; set; }

        [Column("booking_status")]
        public int BookingStatus { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

}
