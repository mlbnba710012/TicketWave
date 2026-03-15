using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;

namespace TicketWave.BackWeb.Controllers
{
    /// <summary>
    /// 表演藝術管理（CRUD）
    /// </summary>
    public class TheaterController : Controller
    {
        private readonly TicketWaveContext _dbContext;
        private readonly ILogger<TheaterController> _logger;

        public TheaterController(TicketWaveContext dbContext, ILogger<TheaterController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        private bool CheckAdminLogin() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername"));

        /// <summary>
        /// 表演藝術列表
        /// </summary>
        public async Task<IActionResult> Index()
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                var theaters = await _dbContext.Theaters
                    .OrderByDescending(t => t.PerformanceDate)
                    .ToListAsync();

                return View(theaters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取表演藝術列表失敗");
                TempData["ErrorMessage"] = "讀取表演藝術列表失敗";
                return View(new List<Theater>());
            }
        }

        /// <summary>
        /// 新增表演藝術頁面
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        /// <summary>
        /// 新增表演藝術處理
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Theater theater)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                ModelState.Remove("Seats");

                if (!ModelState.IsValid)
                    return View(theater);

                theater.TheaterId = Guid.NewGuid();
                theater.AvailableSeats = theater.TotalSeats;
                theater.CreateDate = DateTime.Now;
                theater.UpdateDate = DateTime.Now;

                _dbContext.Theaters.Add(theater);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("管理員新增表演藝術：{TheaterName}", theater.TheaterName);
                TempData["SuccessMessage"] = "表演藝術新增成功";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增表演藝術失敗");
                TempData["ErrorMessage"] = "新增失敗，請稍後再試";
                return View(theater);
            }
        }

        /// <summary>
        /// 編輯表演藝術頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                var theater = await _dbContext.Theaters.FindAsync(id);
                if (theater == null)
                {
                    TempData["ErrorMessage"] = "找不到該表演藝術";
                    return RedirectToAction("Index");
                }

                return View(theater);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取表演藝術資料失敗");
                TempData["ErrorMessage"] = "讀取表演藝術資料失敗";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 編輯表演藝術處理
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Theater theater)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                ModelState.Remove("Seats");

                if (!ModelState.IsValid)
                    return View(theater);

                var existing = await _dbContext.Theaters.FindAsync(theater.TheaterId);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "找不到該表演藝術";
                    return RedirectToAction("Index");
                }

                existing.TheaterName = theater.TheaterName;
                existing.TheaterType = theater.TheaterType;
                existing.Director = theater.Director;
                existing.Duration = theater.Duration;
                existing.AgeRating = theater.AgeRating;
                existing.Language = theater.Language;
                existing.PerformanceDate = theater.PerformanceDate;
                existing.VenueName = theater.VenueName;
                existing.VenueAddress = theater.VenueAddress;
                existing.Description = theater.Description;
                existing.PosterImageUrl = theater.PosterImageUrl;
                existing.Status = theater.Status;
                existing.SaleStartDate = theater.SaleStartDate;
                existing.SaleEndDate = theater.SaleEndDate;
                existing.UpdateDate = DateTime.Now;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("管理員更新表演藝術：{TheaterName}", theater.TheaterName);
                TempData["SuccessMessage"] = "表演藝術更新成功";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新表演藝術失敗");
                TempData["ErrorMessage"] = "更新失敗，請稍後再試";
                return View(theater);
            }
        }

        /// <summary>
        /// 刪除表演藝術頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                var theater = await _dbContext.Theaters.FindAsync(id);
                if (theater == null)
                {
                    TempData["ErrorMessage"] = "找不到該表演藝術";
                    return RedirectToAction("Index");
                }

                var hasOrders = await _dbContext.Orders.AnyAsync(o => o.TheaterId == id);
                ViewBag.HasOrders = hasOrders;

                return View(theater);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取表演藝術資料失敗");
                TempData["ErrorMessage"] = "讀取表演藝術資料失敗";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 刪除表演藝術處理
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                var theater = await _dbContext.Theaters.FindAsync(id);
                if (theater == null)
                {
                    TempData["ErrorMessage"] = "找不到該表演藝術";
                    return RedirectToAction("Index");
                }

                var hasOrders = await _dbContext.Orders.AnyAsync(o => o.TheaterId == id);
                if (hasOrders)
                {
                    TempData["ErrorMessage"] = "此表演藝術已有訂單，無法刪除";
                    return RedirectToAction("Index");
                }

                var seats = await _dbContext.Seats.Where(s => s.TheaterId == id).ToListAsync();
                if (seats.Any())
                {
                    _dbContext.Seats.RemoveRange(seats);
                    _logger.LogInformation("刪除表演藝術相關座位：{SeatCount} 個", seats.Count);
                }

                _dbContext.Theaters.Remove(theater);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("管理員刪除表演藝術：{TheaterName}", theater.TheaterName);
                TempData["SuccessMessage"] = "表演藝術刪除成功";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除表演藝術失敗");
                TempData["ErrorMessage"] = "刪除失敗，請稍後再試";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 查看表演藝術詳情
        /// </summary>
        public async Task<IActionResult> Details(Guid id)
        {
            if (!CheckAdminLogin())
                return RedirectToAction("Login", "Account");

            try
            {
                var theater = await _dbContext.Theaters.FindAsync(id);
                if (theater == null)
                {
                    TempData["ErrorMessage"] = "找不到該表演藝術";
                    return RedirectToAction("Index");
                }

                var seats = await _dbContext.Seats.Where(s => s.TheaterId == id).ToListAsync();
                ViewBag.TotalSeats = seats.Count;
                ViewBag.AvailableSeats = seats.Count(s => s.SeatStatus == 0);
                ViewBag.SoldSeats = seats.Count(s => s.SeatStatus == 1);
                ViewBag.SoldPercentage = seats.Count > 0
                    ? (seats.Count(s => s.SeatStatus == 1) * 100.0 / seats.Count)
                    : 0;

                var orderCount = await _dbContext.Orders
                    .Where(o => o.TheaterId == id)
                    .CountAsync();
                ViewBag.OrderCount = orderCount;

                return View(theater);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取表演藝術詳情失敗");
                TempData["ErrorMessage"] = "讀取表演藝術詳情失敗";
                return RedirectToAction("Index");
            }
        }
    }
}
