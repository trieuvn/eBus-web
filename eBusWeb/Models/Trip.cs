using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace eBusWeb.Models
{
    [Table("Trips")]
    public class Trip : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("route_id")]
        public int RouteId { get; set; }

        [Column("departure_time")]
        public DateTime DepartureTime { get; set; }

        [Column("arrival_time")]
        public DateTime ArrivalTime { get; set; }

        [Column("operator_name")]
        public string OperatorName { get; set; }

        [Column("bus_type")]
        public string BusType { get; set; }

        [Column("base_price")]
        public int BasePrice { get; set; }

        [Column("status")]
        public int Status { get; set; }
    }

}
