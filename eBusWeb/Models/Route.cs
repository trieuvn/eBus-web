using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace eBusWeb.Models
{
    [Table("Routes")]
    public class Route : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("origin")]
        public string Origin { get; set; }

        [Column("destination")]
        public string Destination { get; set; }

        [Column("distance_km")]
        public int DistanceKm { get; set; }

        [Column("estimated_duration")]
        public string EstimatedDuration { get; set; }
    }


}
