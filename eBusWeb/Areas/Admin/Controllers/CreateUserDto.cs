using System.Text.Json.Serialization;
namespace eBusWeb.Areas.Admin.Controllers
{


    public class CreateUserDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public int Role { get; set; }
    }

}
