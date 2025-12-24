using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace eBusWeb.Models
{
    [Table("User")]
    public class User : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("password")]
        public string Password { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        [Column("phone_number")]
        public string PhoneNumber { get; set; }

        [Column("role")]
        public int Role { get; set; }

        [Column("authid")]
        public Guid? AuthId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
