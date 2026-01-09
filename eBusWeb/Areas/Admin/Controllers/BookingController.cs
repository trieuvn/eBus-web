using eBusWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;
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



    }
}
