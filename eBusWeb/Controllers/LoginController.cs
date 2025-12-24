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
        var result = await _supabase
            .From<User>()
            .Get();

        return View(result.Models);
    }
    public async Task<IActionResult> Privacy()
    {
        try
        {
            var result = await _supabase
                .From<User>()
                .Get();

            Console.WriteLine($"Supabase OK - Records: {result.Models.Count}");

            return Content($"Supabase OK - Records: {result.Models.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return Content("Supabase FAIL: " + ex.Message);
        }
    }

}
