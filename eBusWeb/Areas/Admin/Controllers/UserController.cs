using eBusWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace eBusWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly Supabase.Client _supabase;
        private readonly IConfiguration _config;
        public UserController(Supabase.Client supabase, IConfiguration config)
        {
            _supabase = supabase;
            _config = config;
        }

        public async Task<IActionResult> Index(
    string? search,
    int? role,
    int page = 1,
    int pageSize = 5)
        {
            var result = await _supabase
                .From<User>()
                .Order(u => u.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var users = result.Models.AsQueryable();

            // 🔍 Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u =>
                    (!string.IsNullOrEmpty(u.FullName) &&
                     u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    ||
                    (!string.IsNullOrEmpty(u.Email) &&
                     u.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
                );
            }

            // 🎭 Filter role
            if (role.HasValue)
            {
                users = users.Where(u => u.Role == role.Value);
            }

            // 📊 Pagination
            var total = users.Count();

            var data = users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 👉 Truyền thông tin pagination sang View
            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.Role = role;

            return View(data);
        }


        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto model) // Sử dụng DTO ở đây
        {
            if (model == null) return BadRequest("Dữ liệu gửi lên bị trống.");
            if (string.IsNullOrEmpty(model.Email)) return BadRequest("Email không được để trống.");

            try
            {
                using var http = new HttpClient();
                var serviceKey = _config["Supabase:ServiceKey"];
                var supabaseUrl = _config["Supabase:Url"];

                http.DefaultRequestHeaders.Add("apikey", serviceKey);
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceKey);

                // --- BƯỚC 1: TẠO AUTH USER ---
                var payload = new
                {
                    email = model.Email,
                    password = model.Password,
                    email_confirm = true,
                    user_metadata = new
                    {
                        full_name = model.FullName, 
                        phone_number = model.PhoneNumber
                    }
                };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var res = await http.PostAsync($"{supabaseUrl}/auth/v1/admin/users", content);
                var resJson = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                    return StatusCode((int)res.StatusCode, $"Lỗi tạo Auth: {resJson}");

                var authUser = JsonDocument.Parse(resJson).RootElement;
                var authId = Guid.Parse(authUser.GetProperty("id").GetString());
                
                var profile = new User
                {
                    Id = Guid.NewGuid(),
                    AuthId = authId,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Role = model.Role,
                    CreatedAt = DateTime.UtcNow,
                    Password = model.Password
                };

                // Sửa dòng bị lỗi thành:
                await _supabase.From<User>().Insert(profile);


                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi xử lý: {ex.Message}");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var user = await _supabase
                .From<User>()
                .Where(u => u.Id == id)
                .Single();
            if (user == null) return NotFound();

            return Json(new
            {
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                role = user.Role,
                password=user.Password
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var response = await _supabase
                .From<User>()
                .Where(u => u.Id == id)
                .Get();

            var user = response.Models.FirstOrDefault();

            if (user == null)
                return NotFound();

            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] User model)
        {
            try
            {
                await _supabase
                    .From<User>()
                    .Set(u => u.FullName, model.FullName)
                    .Set(u => u.Email, model.Email)
                    .Set(u => u.PhoneNumber, model.PhoneNumber)
                    .Set(u => u.Role, model.Role)
                    .Where(u => u.Id == model.Id)
                    .Update();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _supabase
                    .From<User>()
                    .Where(u => u.Id == id)
                    .Delete();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("Password empty");

              await _supabase
             .From<User>()
             .Set(u => u.Password, dto.NewPassword)
             .Where(u => u.Id == dto.UserId)
             .Update();

            return Ok();
        }


    }
}
