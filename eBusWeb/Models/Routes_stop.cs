using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace eBusWeb.Models
{
    [Table("Routes_stop")]
    public class RouteStop : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("route_id")]
        public int RouteId { get; set; }

        [Column("location_name")]
        public string LocationName { get; set; }

        [Column("stop_type")]
        public int StopType { get; set; }

        [Column("stop_order")]
        public int StopOrder { get; set; }
    }
}
