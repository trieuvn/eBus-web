using eBusWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest;
using Supabase.Postgrest.Interfaces;
using static Supabase.Postgrest.Constants;

namespace eBusWeb.Controllers
{
    [Area("Admin")]
    public class TripController : Controller
    {
        private readonly Supabase.Client _supabaseClient;

        public TripController(Supabase.Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }

        public async Task<IActionResult> Index(
     int page = 1,
     string? search = null,
     int? status = null,
     DateTime? date = null)
        {
            int pageSize = 10;

            try
            {
                // 🔑 FIX TYPE
                IPostgrestTable<Trip> query = _supabaseClient
                    .From<Trip>()
                    .Order(x => x.DepartureTime, Ordering.Ascending);

                // SEARCH
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Filter(
                        x => x.OperatorName, Operator.ILike, $"%{search}%");
                }

                // STATUS
                if (status.HasValue)
                {
                    query = query.Filter(
                        x => x.Status, Operator.Equals, status.Value);
                }

                // DATE
                if (date.HasValue)
                {
                    query = query
                        .Filter(x => x.DepartureTime, Operator.GreaterThanOrEqual, date.Value)
                        .Filter(x => x.DepartureTime, Operator.LessThan, date.Value.AddDays(1));
                }

                // DATA
                var data = await query
                    .Range((page - 1) * pageSize, page * pageSize - 1)
                    .Get();

                // 🔑 COUNT PHẢI DÙNG QUERY MỚI
                IPostgrestTable<Trip> countQuery = _supabaseClient.From<Trip>();

                if (!string.IsNullOrWhiteSpace(search))
                    countQuery = countQuery.Filter(x => x.OperatorName, Operator.ILike, $"%{search}%");

                if (status.HasValue)
                    countQuery = countQuery.Filter(x => x.Status, Operator.Equals, status.Value);

                if (date.HasValue)
                {
                    countQuery = countQuery
                        .Filter(x => x.DepartureTime, Operator.GreaterThanOrEqual, date.Value)
                        .Filter(x => x.DepartureTime, Operator.LessThan, date.Value.AddDays(1));
                }

                var total = await countQuery.Count(CountType.Exact);

                ViewBag.TotalCount = total;
                ViewBag.PageSize = pageSize;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

                return View(data.Models);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi dữ liệu: " + ex.Message;
                return View(new List<Trip>());
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetTripDetail(long id)
        {
            var tripResult = await _supabaseClient
                .From<Trip>()
                .Where(t => t.Id == id)
                .Single();

            if (tripResult == null)
                return NotFound();

            var routeResult = await _supabaseClient
                .From<Models.Route>()
                .Where(r => r.Id == tripResult.RouteId)
                .Single();

            return Json(new
            {
                tripResult.Id,
                tripResult.OperatorName,
                tripResult.BusType,
                tripResult.BasePrice,
                tripResult.Status,
                DepartureTime = tripResult.DepartureTime.ToString("dd/MM/yyyy HH:mm"),
                ArrivalTime = tripResult.ArrivalTime.ToString("dd/MM/yyyy HH:mm"),
                RouteName = routeResult?.Name,
                RouteDuration = routeResult?.EstDuration
            });
        }

        public async Task<IActionResult> Create()
        {
            // Routes
            var routeRes = await _supabaseClient
                .From<Models.Route>()
                .Get();

            ViewBag.Routes = routeRes.Models.ToList();

            // 👇 Lấy BusType DISTINCT từ Trips
            var tripRes = await _supabaseClient
                .From<Models.Trip>()
                .Select("bus_type")
                .Get();

            var busTypes = tripRes.Models
                .Where(t => !string.IsNullOrEmpty(t.BusType))
                .Select(t => t.BusType)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            ViewBag.BusTypes = busTypes;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Trip model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            try
            {
                await _supabaseClient
                    .From<Trip>()
                    .Insert(model);

                return Ok(new
                {
                    success = true,
                    message = "Trip created successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }
}