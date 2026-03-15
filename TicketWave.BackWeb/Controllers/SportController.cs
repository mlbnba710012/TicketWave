using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;

namespace TicketWave.BackWeb.Controllers
{
    /// <summary>
    /// 運動賽事管理（CRUD）
    /// </summary>
    public class SportController : Controller
    {
        private readonly TicketWaveContext _dbContext;
        private readonly ILogger<SportController> _logger;

        public SportController(TicketWaveContext dbContext, ILogger<SportController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        private bool CheckAdminLogin() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername"));

        /// <summary>
        /// 運動賽事列表
        /// </summary>
        public async Task<IActionResult> Index()
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                var sports = await _dbContext.Sports
                    .OrderByDescending(s => s.PerformanceDate)
                    .ToListAsync();

                return View(sports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取運動賽事列表失敗");
                TempData["ErrorMessage"] = "讀取運動賽事列表失敗";
                return View(new List<Sport>());
            }
        }

        /// <summary>
        /// 新增運動賽事頁面
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        /// <summary>
        /// 新增運動賽事處理
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sport sport)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                ModelState.Remove("Seats");

                if (!ModelState.IsValid)
                    return View(sport);

                sport.SportId = Guid.NewGuid();
                sport.AvailableSeats = sport.TotalSeats;
                sport.CreateDate = DateTime.Now;
                sport.UpdateDate = DateTime.Now;

                _dbContext.Sports.Add(sport);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("管理員新增運動賽事：{SportName}", sport.SportName);
                TempData["SuccessMessage"] = "運動賽事新增成功";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增運動賽事失敗");
                TempData["ErrorMessage"] = "新增失敗，請稍後再試";
                return View(sport);
            }
        }

        /// <summary>
        /// 編輯運動賽事頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                var sport = await _dbContext.Sports.FindAsync(id);
                if (sport == null)
                {
                    TempData["ErrorMessage"] = "找不到該運動賽事";
                    return RedirectToAction("Index");
                }

                return View(sport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取運動賽事資料失敗");
                TempData["ErrorMessage"] = "讀取運動賽事資料失敗";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 編輯運動賽事處理
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Sport sport)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                ModelState.Remove("Seats");

                if (!ModelState.IsValid)
                    return View(sport);

                var existing = await _dbContext.Sports.FindAsync(sport.SportId);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "找不到該運動賽事";
                    return RedirectToAction("Index");
                }

                existing.SportName = sport.SportName;
                existing.SportType = sport.SportType;
                existing.HomeTeam = sport.HomeTeam;
                existing.AwayTeam = sport.AwayTeam;
                existing.League = sport.League;
                existing.Season = sport.Season;
                existing.PerformanceDate = sport.PerformanceDate;
                existing.VenueName = sport.VenueName;
                existing.VenueAddress = sport.VenueAddress;
                existing.Description = sport.Description;
                existing.PosterImageUrl = sport.PosterImageUrl;
                existing.Status = sport.Status;
                existing.SaleStartDate = sport.SaleStartDate;
                existing.SaleEndDate = sport.SaleEndDate;
                existing.UpdateDate = DateTime.Now;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("管理員更新運動賽事：{SportName}", sport.SportName);
                TempData["SuccessMessage"] = "運動賽事更新成功";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新運動賽事失敗");
                TempData["ErrorMessage"] = "更新失敗，請稍後再試";
                return View(sport);
            }
        }

        /// <summary>
        /// 刪除運動賽事頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                var sport = await _dbContext.Sports.FindAsync(id);
                if (sport == null)
                {
                    TempData["ErrorMessage"] = "找不到該運動賽事";
                    return RedirectToAction("Index");
                }

                var hasOrders = await _dbContext.Orders.AnyAsync(o => o.SportId == id);
                ViewBag.HasOrders = hasOrders;

                return View(sport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取運動賽事資料失敗");
                TempData["ErrorMessage"] = "讀取運動賽事資料失敗";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 刪除運動賽事處理
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                var sport = await _dbContext.Sports.FindAsync(id);
                if (sport == null)
                {
                    TempData["ErrorMessage"] = "找不到該運動賽事";
                    return RedirectToAction("Index");
                }

                var hasOrders = await _dbContext.Orders.AnyAsync(o => o.SportId == id);
                if (hasOrders)
                {
                    TempData["ErrorMessage"] = "此運動賽事已有訂單，無法刪除";
                    return RedirectToAction("Index");
                }

                var seats = await _dbContext.Seats.Where(s => s.SportId == id).ToListAsync();
                if (seats.Any())
                {
                    _dbContext.Seats.RemoveRange(seats);
                    _logger.LogInformation("刪除運動賽事相關座位：{SeatCount} 個", seats.Count);
                }

                _dbContext.Sports.Remove(sport);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("管理員刪除運動賽事：{SportName}", sport.SportName);
                TempData["SuccessMessage"] = "運動賽事刪除成功";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除運動賽事失敗");
                TempData["ErrorMessage"] = "刪除失敗，請稍後再試";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 查看運動賽事詳情
        /// </summary>
        public async Task<IActionResult> Details(Guid id)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                var sport = await _dbContext.Sports.FindAsync(id);
                if (sport == null)
                {
                    TempData["ErrorMessage"] = "找不到該運動賽事";
                    return RedirectToAction("Index");
                }

                var seats = await _dbContext.Seats.Where(s => s.SportId == id).ToListAsync();
                ViewBag.TotalSeats = seats.Count;
                ViewBag.AvailableSeats = seats.Count(s => s.SeatStatus == 0);
                ViewBag.SoldSeats = seats.Count(s => s.SeatStatus == 1);
                ViewBag.SoldPercentage = seats.Count > 0
                    ? (seats.Count(s => s.SeatStatus == 1) * 100.0 / seats.Count)
                    : 0;

                var orderCount = await _dbContext.Orders
                    .Where(o => o.SportId == id)
                    .CountAsync();
                ViewBag.OrderCount = orderCount;

                return View(sport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取運動賽事詳情失敗");
                TempData["ErrorMessage"] = "讀取運動賽事詳情失敗";
                return RedirectToAction("Index");
            }
        }
    }
}
