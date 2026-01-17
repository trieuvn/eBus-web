using eBusWeb.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Contracts;
using static Supabase.Postgrest.Constants;
using Route = eBusWeb.Models.Route;
namespace eBusWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RouteController : Controller
    {
        
        private readonly Supabase.Client _supabase;
        public RouteController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }
        public async Task<IActionResult> Index(int? id, string? search, int page = 1)
        {
            int pageSize = 6;

            // =============================
            // 1. COUNT
            // =============================
            var countRes = string.IsNullOrWhiteSpace(search)
    ? await _supabase.From<Models.Route>().Get()
    : await _supabase
        .From<Models.Route>()
        .Filter("name", Operator.ILike, $"%{search}%")
        .Get();

            int totalRoutes = countRes.Models.Count;
            int totalPages = (int)Math.Ceiling((double)totalRoutes / pageSize);


            // =============================
            // 2. GET ROUTES
            // =============================
            var response = string.IsNullOrWhiteSpace(search)
                ? await _supabase
                    .From<Models.Route>()
                    .Range((page - 1) * pageSize, (page * pageSize) - 1)
                    .Get()
                : await _supabase
                    .From<Models.Route>()
                    .Filter("name", Operator.ILike, $"%{search}%")
                    .Range((page - 1) * pageSize, (page * pageSize) - 1)
                    .Get();

            var routes = response.Models;

            // =============================
            // 3. SELECTED ROUTE
            // =============================
            Models.Route selectedRoute = null;

            if (id.HasValue)
            {
                selectedRoute = routes.FirstOrDefault(r => r.Id == id.Value);

                if (selectedRoute == null)
                {
                    var selRes = await _supabase
                        .From<Models.Route>()
                        .Filter("id", Operator.Equals, id.Value)
                        .Get();

                    selectedRoute = selRes.Models.FirstOrDefault();
                }
            }
            else
            {
                selectedRoute = routes.FirstOrDefault();
            }

            // =============================
            // 4. LOAD STOPS
            // =============================
            List<RouteStop> stops = new();

            if (selectedRoute != null)
            {
                var stopsResponse = await _supabase
                    .From<RouteStop>()
                    .Filter("route_id", Operator.Equals, selectedRoute.Id)
                    .Order("stop_order", Ordering.Ascending)
                    .Get();

                stops = stopsResponse.Models;
            }

            // =============================
            // 5. VIEWBAG
            // =============================
            ViewBag.Routes = routes;
            ViewBag.SelectedRoute = selectedRoute;
            ViewBag.Stops = stops;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;

            return View();
        }
        public async Task<IActionResult> Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RouteCreateViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Name))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            try
            {
                int totalDuration = (model.Hours * 60) + model.Minutes;

                var newRoute = new Route
                {
                    Name = model.Name,
                    EstDuration = totalDuration
                    // Đảm bảo không gán giá trị cho Id ở đây để DB tự sinh
                };

                // Sử dụng Insert để lấy về kết quả
                var routeResponse = await _supabase.From<Route>().Insert(newRoute);

                // Kiểm tra xem Insert có thành công không
                if (routeResponse.Models.Count == 0)
                {
                    return StatusCode(500, "Không thể tạo Route trong cơ sở dữ liệu.");
                }

                var createdRoute = routeResponse.Models[0];

                if (model.Stops != null && model.Stops.Any())
                {
                    var routeStops = model.Stops.Select(s => new RouteStop
                    {
                        RouteId = createdRoute.Id,
                        LocationName = s.LocationName,
                        StopOrder = s.StopOrder,
                        StopType = s.StopType
                    }).ToList();

                    await _supabase.From<RouteStop>().Insert(routeStops);
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                // Trả về thông báo lỗi chi tiết để bạn thấy ở F12 Console
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }
        [HttpPost]
        public async Task<IActionResult> SaveRouteAndStops([FromBody] RouteWithStopsDto payload)
        {
            if (payload == null) return BadRequest(new { success = false, message = "Invalid data" });

            try
            {
                // 1️⃣ Cập nhật Route
                await _supabase.From<Route>()
                    .Update(new Route { Id = payload.Id, Name = payload.Name, EstDuration = payload.EstDuration });

                // 2️⃣ Cập nhật Stops
                if (payload.Stops != null && payload.Stops.Any())
                {
                    foreach (var stop in payload.Stops)
                    {
                        if (stop.Id == 0)
                            await _supabase.From<RouteStop>().Insert(stop);
                        else
                            await _supabase.From<RouteStop>().Update(stop);
                    }
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DTO
        
        [HttpPost]
        public async Task<IActionResult> DeleteStop([FromBody] int stopId)
        {
            if (stopId <= 0) return BadRequest("Stop ID không hợp lệ");

            try
            {
                var res = await _supabase
                    .From<RouteStop>()
                    .Filter("id", Operator.Equals, stopId)
                    .Get();

                var stop = res.Models.FirstOrDefault();
                if (stop == null) return NotFound("Stop không tồn tại");

                await _supabase.From<RouteStop>().Delete(stop);

                return Ok(new { success = true, message = "Stop deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            // 1. Lấy thông tin Route chi tiết
            var routeRes = await _supabase
                .From<Models.Route>()
                .Filter("id", Operator.Equals, id)
                .Get();

            var selectedRoute = routeRes.Models.FirstOrDefault();

            if (selectedRoute == null)
            {
                return NotFound();
            }

            // 2. Lấy danh sách các Stop thuộc Route này
            var stopsRes = await _supabase
                .From<RouteStop>()
                .Filter("route_id", Operator.Equals, id)
                .Order("stop_order", Ordering.Ascending)
                .Get();

            var stops = stopsRes.Models;

            // 3. Truyền dữ liệu sang View
            ViewBag.SelectedRoute = selectedRoute;
            ViewBag.Stops = stops;

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Delete([FromBody] RouteDeleteDto dto)
        {
            try
            {
                // 1️⃣ Xóa tất cả stops của route
                await _supabase
                    .From<RouteStop>()
                    .Filter("route_id", Operator.Equals, dto.Id)
                    .Delete();

                // 2️⃣ Xóa route
                await _supabase
                    .From<Route>()
                    .Filter("id", Operator.Equals, dto.Id)
                    .Delete();

                // Trả về kết quả thành công
                return Json(new { success = true, message = "Route deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }




        public class RouteDeleteDto
        {
            public int Id { get; set; }
        }

    }
}
