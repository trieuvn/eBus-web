using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace eBusWeb.Models
{
    [Table("Booking_passengers")]
    public class BookingPassenger : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("booking_id")]
        public long BookingId { get; set; }

        [Column("seat_number")]
        public string SeatNumber { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }
    }
}
