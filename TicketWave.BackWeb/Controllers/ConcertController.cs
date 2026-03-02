using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;

namespace TicketWave.BackWeb.Controllers
{
    /// <summary>
    /// 演唱會管理（CRUD）
    /// </summary>
    public class ConcertController : Controller
    {
        private readonly MemberDbContext _dbContext;
        private readonly ILogger<ConcertController> _logger;

        public ConcertController(MemberDbContext dbContext, ILogger<ConcertController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// 演唱會列表
        /// </summary>
        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var concerts = await _dbContext.Concerts
                    .OrderByDescending(c => c.PerformanceDate)
                    .ToListAsync();

                return View(concerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取演唱會列表失敗");
                TempData["ErrorMessage"] = "讀取演唱會列表失敗";
                return View(new List<Concert>());
            }
        }

        /// <summary>
        /// 新增演唱會頁面
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        /// <summary>
        /// 新增演唱會處理
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Concert concert)
        {
            try
            {
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
                {
                    return RedirectToAction("Login", "Account");
                }

                // 移除不需要驗證的導覽屬性
                ModelState.Remove("Seats");

                if (!ModelState.IsValid)
                {
                    return View(concert);
                }

                concert.ConcertId = Guid.NewGuid();
                concert.AvailableSeats = concert.TotalSeats;
                concert.CreateDate = DateTime.Now;
                concert.UpdateDate = DateTime.Now;

                _dbContext.Concerts.Add(concert);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("管理員新增演唱會：{ConcertName}", concert.ConcertName);
                TempData["SuccessMessage"] = "演唱會新增成功";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增演唱會失敗");
                TempData["ErrorMessage"] = "新增失敗，請稍後再試";
                return View(concert);
            }
        }

        /// <summary>
        /// 編輯演唱會頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var concert = await _dbContext.Concerts.FindAsync(id);
                if (concert == null)
                {
                    TempData["ErrorMessage"] = "找不到該演唱會";
                    return RedirectToAction("Index");
                }

                return View(concert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取演唱會資料失敗");
                TempData["ErrorMessage"] = "讀取演唱會資料失敗";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 編輯演唱會處理
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Concert concert)
        {
            try
            {
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
                {
                    return RedirectToAction("Login", "Account");
                }

                // 移除不需要驗證的導覽屬性
                ModelState.Remove("Seats");

                if (!ModelState.IsValid)
                {
                    return View(concert);
                }

                var existingConcert = await _dbContext.Concerts.FindAsync(concert.ConcertId);
                if (existingConcert == null)
                {
                    TempData["ErrorMessage"] = "找不到該演唱會";
                    return RedirectToAction("Index");
                }

                // 更新資料（不包含 TotalSeats 和 AvailableSeats）
                existingConcert.ConcertName = concert.ConcertName;
                existingConcert.ArtistName = concert.ArtistName;
                existingConcert.PerformanceDate = concert.PerformanceDate;
                existingConcert.VenueName = concert.VenueName;
                existingConcert.VenueAddress = concert.VenueAddress;
                existingConcert.Description = concert.Description;
                existingConcert.PosterImageUrl = concert.PosterImageUrl;
                existingConcert.Status = concert.Status;
                existingConcert.SaleStartDate = concert.SaleStartDate;
                existingConcert.SaleEndDate = concert.SaleEndDate;
                existingConcert.UpdateDate = DateTime.Now;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("管理員更新演唱會：{ConcertName}", concert.ConcertName);
                TempData["SuccessMessage"] = "演唱會更新成功";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新演唱會失敗");
                TempData["ErrorMessage"] = "更新失敗，請稍後再試";
                return View(concert);
            }
        }

        /// <summary>
        /// 刪除演唱會頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var concert = await _dbContext.Concerts.FindAsync(id);
                if (concert == null)
                {
                    TempData["ErrorMessage"] = "找不到該演唱會";
                    return RedirectToAction("Index");
                }

                // 檢查是否有訂單
                var hasOrders = await _dbContext.Orders.AnyAsync(o => o.ConcertId == id);
                ViewBag.HasOrders = hasOrders;

                return View(concert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取演唱會資料失敗");
                TempData["ErrorMessage"] = "讀取演唱會資料失敗";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 刪除演唱會處理
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
                {
                    return RedirectToAction("Login", "Account");
                }

                var concert = await _dbContext.Concerts.FindAsync(id);
                if (concert == null)
                {
                    TempData["ErrorMessage"] = "找不到該演唱會";
                    return RedirectToAction("Index");
                }

                // 檢查是否有訂單
                var hasOrders = await _dbContext.Orders.AnyAsync(o => o.ConcertId == id);
                if (hasOrders)
                {
                    TempData["ErrorMessage"] = "此演唱會已有訂單，無法刪除";
                    return RedirectToAction("Index");
                }

                // 刪除相關座位
                var seats = await _dbContext.Seats.Where(s => s.ConcertId == id).ToListAsync();
                if (seats.Any())
                {
                    _dbContext.Seats.RemoveRange(seats);
                    _logger.LogInformation("刪除演唱會相關座位：{SeatCount} 個", seats.Count);
                }

                _dbContext.Concerts.Remove(concert);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("管理員刪除演唱會：{ConcertName}", concert.ConcertName);
                TempData["SuccessMessage"] = "演唱會刪除成功";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除演唱會失敗");
                TempData["ErrorMessage"] = "刪除失敗，請稍後再試";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 查看演唱會詳情
        /// </summary>
        public async Task<IActionResult> Details(Guid id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var concert = await _dbContext.Concerts.FindAsync(id);
                if (concert == null)
                {
                    TempData["ErrorMessage"] = "找不到該演唱會";
                    return RedirectToAction("Index");
                }

                // 查詢座位統計
                var seats = await _dbContext.Seats
                    .Where(s => s.ConcertId == id)
                    .ToListAsync();

                ViewBag.TotalSeats = seats.Count;
                ViewBag.AvailableSeats = seats.Count(s => s.SeatStatus == 0);  // 可購買
                ViewBag.SoldSeats = seats.Count(s => s.SeatStatus == 1);       // 已售出
                ViewBag.ReservedSeats = seats.Count(s => s.SeatStatus == 2);   // 保留中
                ViewBag.LockedSeats = seats.Count(s => s.SeatStatus == 3);     // 已鎖定

                // 計算售出百分比
                ViewBag.SoldPercentage = seats.Count > 0
                    ? (ViewBag.SoldSeats * 100.0 / seats.Count)
                    : 0;

                // 查詢訂單統計
                var orderCount = await _dbContext.Orders
                    .Where(o => o.ConcertId == id)
                    .CountAsync();
                ViewBag.OrderCount = orderCount;

                return View(concert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取演唱會詳情失敗");
                TempData["ErrorMessage"] = "讀取演唱會詳情失敗";
                return RedirectToAction("Index");
            }
        }
    }
}
