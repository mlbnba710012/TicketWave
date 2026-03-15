using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;

namespace TicketWave.BackWeb.Controllers
{
    public class OrderController : Controller
    {
        private readonly TicketWaveContext _dbContext;
        private readonly ILogger<OrderController> _logger;

        public OrderController(TicketWaveContext dbContext, ILogger<OrderController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET: Order/Index
        public async Task<IActionResult> Index(string searchTerm = "", int? status = null, string eventType = "")
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
                return RedirectToAction("Login", "Account");

            try
            {
                var ordersQuery = _dbContext.Orders
                    .Include(o => o.Member)
                    .AsQueryable();

                // 關鍵字搜尋（訂單編號、會員姓名、Email）
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    ordersQuery = ordersQuery.Where(o =>
                        o.OrderNumber.Contains(searchTerm) ||
                        o.Member.Name.Contains(searchTerm) ||
                        o.Member.Email.Contains(searchTerm));
                    ViewBag.SearchTerm = searchTerm;
                }

                // 活動類型篩選
                if (!string.IsNullOrEmpty(eventType))
                {
                    ordersQuery = eventType switch
                    {
                        "concert" => ordersQuery.Where(o => o.ConcertId != null),
                        "sport" => ordersQuery.Where(o => o.SportId != null),
                        "theater" => ordersQuery.Where(o => o.TheaterId != null),
                        _ => ordersQuery
                    };
                    ViewBag.SelectedEventType = eventType;
                }

                // 訂單狀態篩選
                if (status.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.OrderStatus == status.Value);
                    ViewBag.SelectedStatus = status.Value;
                }

                var orders = await ordersQuery
                    .OrderByDescending(o => o.CreateDate)
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取訂單列表失敗");
                TempData["ErrorMessage"] = "讀取訂單列表失敗";
                return View(new List<Order>());
            }
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
                return RedirectToAction("Login", "Account");

            try
            {
                var order = await _dbContext.Orders
                    .Include(o => o.Member)
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "找不到該訂單";
                    return RedirectToAction(nameof(Index));
                }

                // 查詢每筆 OrderDetail 對應的座位資訊
                var seatDict = new Dictionary<Guid, Seat>();
                foreach (var detail in order.OrderDetails)
                {
                    var seat = await _dbContext.Seats
                        .Include(s => s.Concert)
                        .Include(s => s.Sport)
                        .Include(s => s.Theater)
                        .FirstOrDefaultAsync(s => s.OrderDetailId == detail.OrderDetailId);

                    if (seat != null)
                        seatDict[detail.OrderDetailId] = seat;
                }
                ViewBag.SeatDict = seatDict;

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取訂單詳情失敗");
                TempData["ErrorMessage"] = "讀取訂單詳情失敗";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
