using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace eBusWeb.Models
{
    [Table("Payments")]
    public class Payment : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("booking_id")]
        public long BookingId { get; set; }

        [Column("transaction_ref")]
        public string TransactionRef { get; set; }

        [Column("amount")]
        public int Amount { get; set; }

        [Column("payment_method")]
        public string PaymentMethod { get; set; }

        [Column("payment_status")]
        public int PaymentStatus { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

}
