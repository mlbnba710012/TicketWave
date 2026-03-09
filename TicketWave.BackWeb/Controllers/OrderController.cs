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
        public async Task<IActionResult> Index(string searchTerm = "", int? status = null)
        {
            // 檢查登入
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var ordersQuery = _dbContext.Orders
                    .Include(o => o.Member)
                    .AsQueryable();

                // 搜尋功能
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    ordersQuery = ordersQuery.Where(o =>
                        o.OrderId.ToString().Contains(searchTerm) ||
                        o.Member.Name.Contains(searchTerm) ||
                        o.Member.Email.Contains(searchTerm)
                    );
                    ViewBag.SearchTerm = searchTerm;
                }

                // 狀態篩選
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
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var order = await _dbContext.Orders
                    .Include(o => o.Member)
                    .Include(o => o.OrderDetails)
                        //.ThenInclude(od => od.Seat)
                            //.ThenInclude(s => s.Concert)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "找不到該訂單";
                    return RedirectToAction(nameof(Index));
                }

                // 手動查詢座位資訊
                foreach (var detail in order.OrderDetails)
                {
                    // 透過 OrderDetailId 反查座位
                    var seat = await _dbContext.Seats
                        .Include(s => s.Concert)
                        .FirstOrDefaultAsync(s => s.OrderDetailId == detail.OrderDetailId);

                    // 暫存在 ViewBag 或建立 ViewModel
                    // ViewBag.Seats 或使用字典存儲
                }

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

