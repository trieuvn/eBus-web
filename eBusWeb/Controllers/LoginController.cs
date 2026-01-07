using Microsoft.AspNetCore.Mvc;
using Supabase;
using eBusWeb.Models;

public class LoginController : Controller
{
    private readonly Client _supabase;

    public LoginController(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<IActionResult> Index()
    {


        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Index([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _supabase
                .From<User>()
                .Where(u => u.Email == request.Email)
                .Get();

            if (result.Models.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Email không tồn tại"
                });
            }

            var user = result.Models.First();
            if (user.Role != 1)
            {
                return Json(new
                {
                    success = false,
                    message = "Tài khoản không có quyền truy cập"
                });
            }
            // ⚠ Demo: password plain text
            if (user.Password != request.Password)
            {
                return Json(new
                {
                    success = false,
                    message = "Sai mật khẩu"
                });
            }
            HttpContext.Session.SetString("FullName", user.FullName);

            return Json(new
            {
                success = true,
                redirectUrl = "/Admin/Dashboard"
            });

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return Json(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

}

