using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;

namespace TicketWave.BackWeb.Controllers
{
    public class DashboardController : Controller
    {
        private readonly TicketWaveContext _dbContext;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(TicketWaveContext dbContext, ILogger<DashboardController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
                return RedirectToAction("Login", "Account");

            try
            {
                // 初始化預設值
                ViewBag.TotalMembers = 0;
                ViewBag.TotalConcerts = 0;
                ViewBag.TotalSports = 0;
                ViewBag.TotalTheaters = 0;
                ViewBag.TotalOrders = 0;
                ViewBag.TotalRevenue = 0;
                ViewBag.TodayOrders = 0;
                ViewBag.TodayRevenue = 0;
                ViewBag.RecentOrders = new List<Order>();
                ViewBag.PopularConcerts = new List<Concert>();
                ViewBag.PopularSports = new List<Sport>();
                ViewBag.PopularTheaters = new List<Theater>();

                try { ViewBag.TotalMembers = await _dbContext.Members.CountAsync(); }
                catch (Exception ex) { _logger.LogWarning(ex, "無法讀取會員總數"); }

                try { ViewBag.TotalConcerts = await _dbContext.Concerts.CountAsync(); }
                catch (Exception ex) { _logger.LogWarning(ex, "無法讀取演唱會總數"); }

                try { ViewBag.TotalSports = await _dbContext.Sports.CountAsync(); }
                catch (Exception ex) { _logger.LogWarning(ex, "無法讀取運動賽事總數"); }

                try { ViewBag.TotalTheaters = await _dbContext.Theaters.CountAsync(); }
                catch (Exception ex) { _logger.LogWarning(ex, "無法讀取表演藝術總數"); }

                try
                {
                    ViewBag.TotalOrders = await _dbContext.Orders.CountAsync();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "無法讀取訂單總數"); }

                try
                {
                    ViewBag.TotalRevenue = await _dbContext.Orders
                        .Where(o => o.OrderStatus == 1)
                        .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                }
                catch (Exception ex) { _logger.LogWarning(ex, "無法計算總營收"); }

                try
                {
                    var today = DateTime.Today;
                    ViewBag.TodayOrders = await _dbContext.Orders
                        .Where(o => o.CreateDate >= today)
                        .CountAsync();

                    ViewBag.TodayRevenue = await _dbContext.Orders
                        .Where(o => o.CreateDate >= today && o.OrderStatus == 1)
                        .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                }
                catch (Exception ex) { _logger.LogWarning(ex, "無法讀取今日統計"); }

                try
                {
                    ViewBag.RecentOrders = await _dbContext.Orders
                        .Include(o => o.Member)
                        .OrderByDescending(o => o.CreateDate)
                        .Take(10)
                        .ToListAsync();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "無法讀取最新訂單"); }

                try
                {
                    ViewBag.PopularConcerts = await _dbContext.Concerts
                        .OrderByDescending(c => c.TotalSeats - c.AvailableSeats)
                        .Take(5)
                        .ToListAsync();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "無法讀取熱門演唱會"); }

                try
                {
                    ViewBag.PopularSports = await _dbContext.Sports
                        .OrderByDescending(s => s.TotalSeats - s.AvailableSeats)
                        .Take(5)
                        .ToListAsync();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "無法讀取熱門運動賽事"); }

                try
                {
                    ViewBag.PopularTheaters = await _dbContext.Theaters
                        .OrderByDescending(t => t.TotalSeats - t.AvailableSeats)
                        .Take(5)
                        .ToListAsync();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "無法讀取熱門表演藝術"); }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入儀表板時發生嚴重錯誤");

                ViewBag.TotalMembers = 0;
                ViewBag.TotalConcerts = 0;
                ViewBag.TotalSports = 0;
                ViewBag.TotalTheaters = 0;
                ViewBag.TotalOrders = 0;
                ViewBag.TotalRevenue = 0;
                ViewBag.TodayOrders = 0;
                ViewBag.TodayRevenue = 0;
                ViewBag.RecentOrders = new List<Order>();
                ViewBag.PopularConcerts = new List<Concert>();
                ViewBag.PopularSports = new List<Sport>();
                ViewBag.PopularTheaters = new List<Theater>();

                TempData["ErrorMessage"] = "部分資料載入失敗，顯示預設值";
                return View();
            }
        }
    }
}
