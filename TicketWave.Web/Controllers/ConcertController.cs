using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;
using TicketWave.Service.Services.Interface;

namespace TicketWave.Web.Controllers
{
    public class ConcertController : Controller
    {
        private readonly MemberDbContext _dbContext;
        private readonly IOrderService _orderService;
        private readonly ILogger<ConcertController> _logger;

        public ConcertController(
            MemberDbContext dbContext,
            IOrderService orderService,
            ILogger<ConcertController> logger)
        {
            _dbContext = dbContext;
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// 演唱會列表
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var concerts = await _dbContext.Concerts
                .Where(c => c.Status == 1) // 只顯示售票中的演唱會
                .OrderBy(c => c.PerformanceDate)
                .ToListAsync();

            return View(concerts);
        }

        /// <summary>
        /// 演唱會詳情
        /// </summary>
        public async Task<IActionResult> Details(Guid id)
        {
            var concert = await _dbContext.Concerts.FindAsync(id);

            if (concert == null)
            {
                return NotFound();
            }

            // 查詢各區域的座位資訊
            var seatZones = await _dbContext.Seats
                .Where(s => s.ConcertId == id)
                .GroupBy(s => s.SeatZone)
                .Select(g => new
                {
                    Zone = g.Key,
                    MinPrice = g.Min(s => s.Price),
                    MaxPrice = g.Max(s => s.Price),
                    TotalSeats = g.Count(),
                    AvailableSeats = g.Count(s => s.SeatStatus == 0)
                })
                .ToListAsync();

            ViewBag.SeatZones = seatZones;

            return View(concert);
        }

        /// <summary>
        /// 選座頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SelectSeats(Guid concertId)
        {
            try
            {
                // 檢查是否登入
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    TempData["ErrorMessage"] = "請先登入才能購票";
                    return RedirectToAction("Login", "Member");
                }

                var memberId = Guid.Parse(memberIdStr);

                // 查詢演唱會資訊
                var concert = await _dbContext.Concerts.FindAsync(concertId);
                if (concert == null)
                {
                    TempData["ErrorMessage"] = "找不到此演唱會";
                    return RedirectToAction("Concerts", "Home");
                }

                // 查詢該會員已購買的票數
                var currentTicketCount = await _orderService.GetMemberTicketCountForConcert(memberId, concertId);

                // 計算剩餘可購買數量
                var remainingQuota = 4 - currentTicketCount;

                // 設定 ViewBag
                ViewBag.Concert = concert;
                ViewBag.CurrentTicketCount = currentTicketCount;
                ViewBag.RemainingQuota = remainingQuota;

                _logger.LogInformation($"會員 {memberId} 進入選座頁面，已購買 {currentTicketCount} 張，剩餘 {remainingQuota} 張");

                // 查詢所有座位
                var seats = await _dbContext.Seats
                    .Where(s => s.ConcertId == concertId)
                    .OrderBy(s => s.SeatZone)
                    .ThenBy(s => s.SeatRow)
                    .ThenBy(s => s.SeatNumber)
                    .ToListAsync();

                // 取得會員已購買的座位
                var memberSeatIds = await _dbContext.OrderDetails
                    .Where(od => od.Order.MemberId == memberId
                              && od.Order.ConcertId == concertId
                              && od.Order.OrderStatus != 2)
                    .Join(_dbContext.Seats,
                          od => od.OrderDetailId,
                          s => s.OrderDetailId,
                          (od, s) => s.SeatId)
                    .ToListAsync();

                ViewBag.MemberSeatIds = memberSeatIds;
                _logger.LogInformation($"會員 {memberId} 已購買的座位數：{memberSeatIds.Count}");


                return View(seats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "選座頁面載入失敗");
                TempData["ErrorMessage"] = "載入頁面時發生錯誤";
                return RedirectToAction("Concerts", "Home");
                //return Content(ex.ToString());
                //return RedirectToAction("Index", "Concert");
            }
        }

        /// <summary>
        /// 提交選座（建立訂單）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                // 檢查是否登入
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    return Json(new { success = false, message = "請先登入" });
                }

                var memberId = Guid.Parse(memberIdStr);

                // 驗證座位數量
                if (request.SelectedSeats == null || !request.SelectedSeats.Any())
                {
                    return Json(new { success = false, message = "請選擇至少一個座位" });
                }

                if (request.SelectedSeats.Count > 4)
                {
                    return Json(new { success = false, message = "一次最多只能選擇 4 個座位" });
                }

                // 建立訂單
                var result = await _orderService.CreateOrder(memberId, request.ConcertId, request.SelectedSeats);

                if (result.Success)
                {
                    _logger.LogInformation($"訂單建立成功：會員 {memberId}，訂單 {result.OrderId}");
                    return Json(new
                    {
                        success = true,
                        message = result.Message,
                        orderId = result.OrderId
                    });
                }
                else
                {
                    _logger.LogWarning($"訂單建立失敗：{result.Message}");
                    return Json(new
                    {
                        success = false,
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立訂單時發生錯誤");
                return Json(new
                {
                    success = false,
                    message = "訂單建立失敗，請稍後再試"
                });
            }
        }

        /// <summary>
        /// 檢查座位狀態（AJAX）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckSeatStatus(Guid concertId)
        {
            try
            {
                var seats = await _dbContext.Seats
                    .Where(s => s.ConcertId == concertId)
                    .Select(s => new
                    {
                        seatId = s.SeatId,
                        zone = s.SeatZone,
                        row = s.SeatRow,
                        number = s.SeatNumber,
                        status = s.SeatStatus,
                        price = s.Price
                    })
                    .ToListAsync();

                return Json(seats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查座位狀態失敗");
                return Json(new List<object>());
            }
        }
    }

    /// <summary>
    /// 建立訂單請求模型
    /// </summary>
    public class CreateOrderRequest
    {
        public Guid ConcertId { get; set; }
        public List<Guid> SelectedSeats { get; set; }
    }
}
