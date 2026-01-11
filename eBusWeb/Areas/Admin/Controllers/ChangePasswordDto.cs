namespace eBusWeb.Areas.Admin.Controllers
{
    public class ChangePasswordDto
    {
        public Guid UserId { get; set; }
        public string NewPassword { get; set; }
    }

}