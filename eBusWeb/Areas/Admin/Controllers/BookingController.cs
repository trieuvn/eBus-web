using eBusWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace eBusWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookingController : Controller
    {
        private readonly Supabase.Client _supabase;

        public BookingController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
        {
            var bookingsTable = _supabase.From<Booking>();

            // --- Phân trang ---
            if (page < 1) page = 1;
            int offset = (page - 1) * pageSize;

            var bookingsResponse = await bookingsTable
                .Range(offset, offset + pageSize - 1)
                .Get();

            var bookings = bookingsResponse.Models ?? new List<Booking>();

            // Tổng số bản ghi
            int totalRecords = await bookingsTable.Count(CountType.Exact);
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            if (totalPages < 1) totalPages = 1;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            // --- Statistics ---
            ViewBag.TotalBookings = totalRecords;

            // Revenue Today (lọc created_at >= today)
            
            var revenueResponse = await bookingsTable
                .Select("total_amount")
                .Get();

            ViewBag.RevenueToday = revenueResponse.Models?.Sum(b => b.TotalAmount) ?? 0;

            // Pending bookings (booking_status = 0)
            ViewBag.PendingCount = await bookingsTable
                .Filter("booking_status", Operator.Equals, 0)
                .Count(CountType.Exact);

            // Seats Sold
            var passengersTable = _supabase.From<BookingPassenger>();
            ViewBag.SeatsSold = await passengersTable.Count(CountType.Exact);

            return View(bookings);
        }
        public async Task<IActionResult> Edit(long id)
        {
            // 1. Booking
            var booking = await _supabase
                .From<Booking>()
                .Where(b => b.Id == id)
                .Single();

            if (booking == null)
                return NotFound();

            // 2. User
            var user = await _supabase
                .From<User>()
                .Where(u => u.Id == booking.UserId)
                .Single();

            // 3. Trip
            var trip = await _supabase
                .From<Trip>()
                .Where(t => t.Id == booking.TripId)
                .Single();

            // 4. Passengers
            var passengersRes = await _supabase
                .From<BookingPassenger>()
                .Where(p => p.BookingId == booking.Id)
                .Get();

            // 5. Payment
            var paymentRes = await _supabase
                .From<Payment>()
                .Where(p => p.BookingId == booking.Id)
                .Get();

            var payment = paymentRes.Models.FirstOrDefault();


            // 6. RouteStops theo Route của Trip
            var routeStopsRes = await _supabase
                .From<RouteStop>()
                .Where(rs => rs.RouteId == trip.RouteId)
                .Order(rs => rs.StopOrder, Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var routeStops = routeStopsRes.Models;

            // ✅ Quy ước logic
            // Pickup: không lấy stop cuối
            // Dropoff: không lấy stop đầu
            var pickupStops = routeStops.Take(routeStops.Count - 1).ToList();
            var dropoffStops = routeStops.Skip(1).ToList();
            var route = await _supabase
                .From<Models.Route>()
                .Where(r => r.Id == trip.RouteId)
                .Single();
            // 7. ViewModel
            var vm = new BookingEditVM
            {
                Booking = booking,
                User = user,
                Trip = trip,
                Route = route,
                Passengers = passengersRes.Models,
                Payment = payment,
                AvailablePickupStops = pickupStops,
                AvailableDropoffStops = dropoffStops
            };

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(BookingEditVM model)
        {
            try
            {
                if (model?.Booking == null)
                    return Json(new { success = false, message = "INVALID_DATA" });

                if (model.Booking.PickupStopId == model.Booking.DropoffStopId)
                    return Json(new { success = false, message = "INVALID_STOP_SELECTION" });

                // ✅ UPDATE BOOKING (CÁCH AN TOÀN NHẤT)
                await _supabase
                    .From<Booking>()
                    .Where(b => b.Id == model.Booking.Id)
                    .Set(b => b.PickupStopId, model.Booking.PickupStopId)
                    .Set(b => b.DropoffStopId, model.Booking.DropoffStopId)
                    .Set(b => b.ContactName, model.Booking.ContactName)
                    .Set(b => b.ContactEmail, model.Booking.ContactEmail)
                    .Set(b => b.BookingStatus, model.Booking.BookingStatus)
                    .Set(b => b.TotalAmount, model.Booking.TotalAmount)
                    .Update();

                // ✅ UPDATE PAYMENT
                if (model.Payment != null)
                {
                    await _supabase
                        .From<Payment>()
                        .Where(p => p.BookingId == model.Booking.Id)
                        .Set(p => p.Amount, model.Payment.Amount)
                        .Set(p => p.PaymentStatus, model.Payment.PaymentStatus)
                        .Update();
                }
                return Json(new
                {
                    success = true,
                    message = "BOOKING_UPDATED",
                    redirectUrl = "/Admin/Booking"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> Details(long bookingId)
        {
            try
            {
                // Sử dụng .Get() thay vì .Single() để kiểm soát lỗi tốt hơn
                var bookingResponse = await _supabase.From<Booking>()
                    .Where(b => b.Id == bookingId)
                    .Get();

                var currentBooking = bookingResponse.Model;
                if (currentBooking == null)
                    return Json(new { success = false, message = "Booking không tồn tại." });

                // KHÔNG lấy thông tin User nếu bạn muốn tránh lỗi Guid
                // Hoặc nếu lấy, phải kiểm tra cực kỳ kỹ:
                object userInfo = null;
                if (currentBooking.UserId != null && currentBooking.UserId != Guid.Empty)
                {
                    // Chỉ truy vấn khi UserId thực sự là một Guid hợp lệ
                    var userRes = await _supabase.From<User>().Where(u => u.Id == currentBooking.UserId).Get();
                    if (userRes.Model != null)
                    {
                        userInfo = new { id = userRes.Model.Id, fullName = userRes.Model.FullName };
                    }
                }

                var passengersResponse = await _supabase.From<BookingPassenger>()
                    .Where(p => p.BookingId == bookingId)
                    .Get();

                return Json(new
                {
                    success = true,
                    bookingId = currentBooking.Id,
                    bookingContactName = currentBooking.ContactName,
                    user = userInfo,
                    passengers = passengersResponse.Models.Select(p => new {
                        id = p.Id,
                        fullName = p.FullName ?? "Unknown",
                        seatNumber = p.SeatNumber
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> Delete(long bookingId)
        {
            try
            {
                // 1️⃣ Kiểm tra booking tồn tại
                var bookingResponse = await _supabase
                    .From<Booking>()
                    .Where(b => b.Id == bookingId)
                    .Get();

                var booking = bookingResponse.Model;

                if (booking == null)
                    return Json(new { success = false, message = "Booking không tồn tại." });

                // 2️⃣ Xóa tất cả passengers liên quan trước (nếu có)
                var passengersTable = _supabase.From<BookingPassenger>();
                await passengersTable
                    .Where(p => p.BookingId == bookingId)
                    .Delete();

                // 3️⃣ Xóa payment liên quan (nếu có)
                var paymentTable = _supabase.From<Payment>();
                await paymentTable
                    .Where(p => p.BookingId == bookingId)
                    .Delete();

                // 4️⃣ Cuối cùng xóa booking
                await _supabase
                    .From<Booking>()
                    .Where(b => b.Id == bookingId)
                    .Delete();

                return Json(new { success = true, message = "Xóa booking thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        public async Task<IActionResult> Create()
        {
            var routesRes = await _supabase
                .From<Models.Route>()
                .Get();

            ViewBag.Routes = routesRes.Models;
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetStopsByRoute(int routeId)
        {
            try
            {
                if (routeId <= 0)
                {
                    return BadRequest(new { success = false, message = "Route ID không hợp lệ" });
                }

                // Lấy danh sách điểm dừng theo routeId
                var stopsResponse = await _supabase
                    .From<RouteStop>()
                    .Where(x => x.RouteId == routeId)
                    .Get();

                var stops = stopsResponse.Models ?? new List<RouteStop>();

                if (stops.Count == 0)
                {
                    // Kiểm tra route tồn tại (an toàn, không dùng .Single() để tránh exception)
                    var routeResponse = await _supabase
                        .From<Models.Route>()
                        .Where(r => r.Id == routeId)
                        .Get();

                    var route = routeResponse.Model; // null nếu không tìm thấy hoặc có lỗi mapping

                    if (route == null)
                    {
                        return NotFound(new { success = false, message = $"Không tìm thấy tuyến đường ID {routeId}" });
                    }

                    return Ok(new
                    {
                        success = true,
                        stops = new List<RouteStop>(),
                        message = "Tuyến này chưa có điểm dừng"
                    });
                }

                // Sắp xếp theo stop_order
                var orderedStops = stops
                    .OrderBy(s => s.StopOrder)
                    .ToList();
                return Ok(new
                {
                    success = true,
                    stops = orderedStops.Select(s => new
                    {
                        id = s.Id,
                        name = s.LocationName,
                        stopType = s.StopType,
                        order = s.StopOrder
                    })
                });
            }
            catch (PostgrestException pgEx)
            {
                // Lỗi phổ biến nhất: mapping model thất bại (tên bảng/cột sai, RLS...)
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi Supabase (PostgrestException): " + pgEx.Message,
                    details = pgEx.Reason.ToString() ?? "Không có chi tiết",
                    content = pgEx.Content ?? "No content",
                    hint = "Kiểm tra: tên bảng Routes_stop có đúng? cột route_id, location_name, stop_order có khớp model?"
                });
            }
            catch (Exception ex)
            {
                // Lỗi chung (kết nối, timeout, JSON parse...)
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server: " + ex.Message,
                    innerException = ex.InnerException?.Message ?? "Không có chi tiết",
                    stackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0)) ?? "No stack"
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetForSelect2(string q)
        {
            var query = _supabase.From<User>();

            if (!string.IsNullOrEmpty(q))
            {
                query = (Supabase.Interfaces.ISupabaseTable<User, Supabase.Realtime.RealtimeChannel>)query.Filter("full_name", Operator.ILike, $"%{q}%");
            }

            var res = await query.Limit(10).Get();

            // Đảm bảo trả về đúng định dạng mà Select2 cần: { id, text }
            var results = res.Models.Select(u => new {
                id = u.Id, // Đây phải là Guid/String khớp với bảng User
                text = u.FullName + " (" + u.Email + ")"
            });

            return Json(results);
        }
        
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BookingCreateVM model)
        {
            try
            {
                if (model == null)
                    return Json(new { success = false, message = "INVALID_DATA" });

                // 1. Tạo Booking entity
                var booking = new Booking
                {
                    UserId = model.UserId,
                    TripId=model.TripId,
                    PickupStopId = model.PickupStopId,
                    DropoffStopId = model.DropoffStopId,
                    ContactName = model.ContactName,
                    ContactMobile = model.ContactMobile,
                    ContactEmail = model.ContactEmail,
                    TotalAmount = model.TotalAmount,
                    BookingStatus = 1, // Pending
                    CreatedAt = DateTime.UtcNow
                };

                // 2. Insert Booking
                var bookingRes = await _supabase
                    .From<Booking>()
                    .Insert(booking);

                var createdBooking = bookingRes.Models.FirstOrDefault();
                if (createdBooking == null)
                    return Json(new { success = false, message = "CREATE_FAILED" });

                // 3. Insert Passengers
                if (model.Passengers?.Any() == true)
                {
                    foreach (var p in model.Passengers)
                        p.BookingId = createdBooking.Id;

                    await _supabase
                        .From<BookingPassenger>()
                        .Insert(model.Passengers);
                }

                return Json(new
                {
                    success = true,
                    message = "BOOKING_CREATED",
                    redirectUrl = "/Admin/Booking"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetTripsByRoute(long routeId)
        {
            try
            {
                if (routeId <= 0)
                {
                    return Json(new { success = false, trips = new object[] { } });
                }

                var tripsRes = await _supabase
                    .From<Trip>()
                    .Where(t => t.RouteId == routeId)
                    .Where(t => t.Status == 1) // ACTIVE
                    .Order(t => t.DepartureTime, Ordering.Ascending)
                    .Get();

                var trips = tripsRes.Models ?? new List<Trip>();

                return Json(new
                {
                    success = true,
                    trips = trips.Select(t => new
                    {
                        id = t.Id,
                        departureTime = t.DepartureTime.ToString("yyyy-MM-dd HH:mm"),
                        busType = t.BusType
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPassengersByBooking(long bookingId)
        {
            try
            {
                // 1️⃣ Lấy thông tin booking
                var bookingTable = _supabase.From<Booking>();
                var booking = await bookingTable.Where(b => b.Id == bookingId).Single();

                if (booking == null)
                    return Json(new { success = false, message = "Booking not found." });

                // 2️⃣ Lấy thông tin user chính của booking
                User userInfo = null;
                if (booking.UserId != Guid.Empty)
                {
                    var userTable = _supabase.From<User>();
                    userInfo = await userTable.Where(u => u.Id == booking.UserId).Single();
                }

                // 3️⃣ Lấy danh sách hành khách trong booking
                var passengersTable = _supabase.From<BookingPassenger>();
                var passengersResponse = await passengersTable.Where(p => p.BookingId == bookingId).Get();
                var passengers = passengersResponse.Models.Select(p => new
                {
                    p.Id,
                    FullName = string.IsNullOrEmpty(p.FullName) ? "Unknown" : p.FullName,
                    p.SeatNumber
                }).ToList();

                // 4️⃣ Lấy tất cả users
                var allUsersTable = _supabase.From<User>();
                var allUsersResponse = await allUsersTable.Get();
                var allUsers = allUsersResponse.Models.ToList();

                // 5️⃣ Lọc ra những user chưa được chọn trong booking (loại bỏ user chính)
                var availableUsers = allUsers
                    .Where(u => booking.UserId == Guid.Empty || u.Id != booking.UserId)
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Email
                    })
                    .ToList();

                // 6️⃣ Trả dữ liệu
                return Json(new
                {
                    success = true,
                    bookingId,
                    bookingContactName = booking.ContactName,
                    user = userInfo != null ? new { userInfo.Id, userInfo.FullName } : null,
                    passengers,
                    availableUsers
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }

}
