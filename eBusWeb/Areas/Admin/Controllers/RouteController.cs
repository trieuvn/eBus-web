using Microsoft.AspNetCore.Mvc;
using static Supabase.Postgrest.Constants;

// 👉 Alias tránh trùng Route hệ thống
using RouteModel = eBusWeb.Models.Route;
using eBusWeb.Models;

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

        // ======================================================
        // INDEX
        // ======================================================
        public async Task<IActionResult> Index(int? id, string? search, int page = 1)
        {
            const int pageSize = 6;

            // 1. Tạo Query cơ bản
            var query = _supabase.From<RouteModel>();

            // 2. Áp dụng Filter nếu có search
            if (!string.IsNullOrWhiteSpace(search))
            {
                // Sử dụng ILike để tìm kiếm gần đúng (tương đương CONTAINS)
                query = (Supabase.Interfaces.ISupabaseTable<RouteModel, Supabase.Realtime.RealtimeChannel>)query.Filter("name", Operator.ILike, $"%{search}%");
            }

            // 3. Thực hiện lấy Count và Data cùng lúc hoặc tuần tự đúng cách
            // Lưu ý: PostgREST có hỗ trợ đếm trực tiếp trong lúc lấy data để tiết kiệm request
            var routesRes = await query
                .Range((page - 1) * pageSize, (page * pageSize) - 1)
                .Order("id", Ordering.Descending) // Nên có Order khi dùng Paging
                .Get();

            var routes = routesRes.Models;

            // Lấy tổng số dòng từ Response (Nếu Supabase trả về Count) 
            // Hoặc query riêng một lần nữa nếu cần chính xác total
            var totalRoutesRes = await query.Get();
            int totalRoutes = totalRoutesRes.Models.Count;

            int totalPages = (int)Math.Ceiling(totalRoutes / (double)pageSize);

            // 4. Logic lấy SelectedRoute
            RouteModel selectedRoute = null;
            if (id.HasValue)
            {
                selectedRoute = routes.FirstOrDefault(r => r.Id == id.Value)
                    ?? (await _supabase.From<RouteModel>().Where(r => r.Id == id.Value).Get()).Models.FirstOrDefault();
            }
            else
            {
                selectedRoute = routes.FirstOrDefault();
            }

            // 5. Load Stops
            var stops = new List<RouteStop>();
            if (selectedRoute != null)
            {
                var stopsRes = await _supabase.From<RouteStop>()
                    .Where(s => s.RouteId == selectedRoute.Id)
                    .Order("stop_order", Ordering.Ascending)
                    .Get();
                stops = stopsRes.Models;
            }

            // Gán ViewBag
            ViewBag.Routes = routes;
            ViewBag.SelectedRoute = selectedRoute;
            ViewBag.Stops = stops;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;

            return View();
        }

        // ======================================================
        // CREATE
        // ======================================================
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RouteCreateViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("Invalid route data");

            try
            {
                var route = new RouteModel
                {
                    Name = model.Name,
                    Origin = model.Origin,
                    Destination = model.Destination,
                    DistanceKm = model.DistanceKm,
                    EstimatedDuration = $"{model.Hours}h {model.Minutes}m"
                };

                var insertRes = await _supabase
                    .From<RouteModel>()
                    .Insert(route);

                var createdRoute = insertRes.Models.First();

                if (model.Stops?.Any() == true)
                {
                    // 🔥 FIX: IEnumerable → ICollection
                    var stops = model.Stops
                        .Select(s => new RouteStop
                        {
                            RouteId = createdRoute.Id,
                            LocationName = s.LocationName,
                            StopType = s.StopType,
                            StopOrder = s.StopOrder
                        })
                        .ToList();

                    await _supabase
                        .From<RouteStop>()
                        .Insert(stops);
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ======================================================
        // UPDATE ROUTE + STOPS
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> SaveRouteAndStops([FromBody] RouteWithStopsDto dto)
        {
            if (dto == null) return BadRequest();

            try
            {
                // UPDATE ROUTE
                await _supabase
                    .From<RouteModel>()
                    .Where(r => r.Id == dto.Id)
                    .Update(new RouteModel
                    {
                        Name = dto.Name,
                        Origin = dto.Origin,
                        Destination = dto.Destination,
                        DistanceKm = dto.DistanceKm,
                        EstimatedDuration = dto.EstimatedDuration
                    });

                // UPSERT STOPS
                if (dto.Stops != null)
                {
                    foreach (var stop in dto.Stops)
                    {
                        if (stop.Id <= 0)
                        {
                            await _supabase
                                .From<RouteStop>()
                                .Insert(stop);
                        }
                        else
                        {
                            await _supabase
                                .From<RouteStop>()
                                .Where(s => s.Id == stop.Id)
                                .Update(stop);
                        }
                    }
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ======================================================
        // DELETE STOP
        // ======================================================
        // 1. Tạo class hứng dữ liệu
        public class DeleteRequest
        {
            public int Id { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStop([FromBody] DeleteRequest request)
        {
            // Sử dụng request.Id thay vì stopId
            if (request == null || request.Id <= 0) return BadRequest(new { success = false, message = "Invalid ID" });

            try
            {
                await _supabase
                    .From<RouteStop>()
                    .Where(s => s.Id == request.Id)
                    .Delete();

                return Ok(new { success = true, message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ======================================================
        // EDIT
        // ======================================================
        public async Task<IActionResult> Edit(int id)
        {
            var routeRes = await _supabase
                .From<RouteModel>()
                .Where(r => r.Id == id)
                .Get();

            var route = routeRes.Models.FirstOrDefault();
            if (route == null) return NotFound();

            var stopsRes = await _supabase
                .From<RouteStop>()
                .Where(s => s.RouteId == id)
                .Order("stop_order", Ordering.Ascending)
                .Get();

            ViewBag.SelectedRoute = route;
            ViewBag.Stops = stopsRes.Models;

            return View();
        }

        // ======================================================
        // DELETE ROUTE
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] RouteDeleteDto dto)
        {
            if (dto == null || dto.Id <= 0)
                return BadRequest();

            try
            {
                await _supabase
                    .From<RouteStop>()
                    .Where(s => s.RouteId == dto.Id)
                    .Delete();

                await _supabase
                    .From<RouteModel>()
                    .Where(r => r.Id == dto.Id)
                    .Delete();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // ======================================================
        // DTO
        // ======================================================
        public class RouteDeleteDto
        {
            public int Id { get; set; }
        }
    }
}
