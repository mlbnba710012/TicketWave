using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using System;
using TicketWave.Repository.Entity;
using TicketWave.Service.Services.Interface;
using TicketWave.Web.Models;


namespace TicketWave.Web.Controllers
{
    public class SportController : Controller
    {
        private readonly TicketWaveContext _dbContext;
        private readonly IOrderService _orderService;
        private readonly ILogger<SportController> _logger;

        public SportController(
            TicketWaveContext dbContext,
            IOrderService orderService,
            ILogger<SportController> logger)
        {
            _dbContext = dbContext;
            _orderService = orderService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var sports = await _dbContext.Sports
                .Where(c => c.Status == 1)
                .OrderBy(c => c.DisplayOrder)
                //.OrderBy(c => c.PerformanceDate)
                .ToListAsync();

            return View(sports);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var sport = await _dbContext.Sports.FindAsync(id);
            if (sport == null) return NotFound();

            var seatZones = await _dbContext.Seats
                .Where(s => s.SportId == id)
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
            return View(sport);
        }

        [HttpGet]
        public async Task<IActionResult> SelectZone(Guid sportId)
        {
            try
            {
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    TempData["ErrorMessage"] = "請先登入才能購票";
                    return RedirectToAction("Login", "Member");
                }

                var sport = await _dbContext.Sports.FindAsync(sportId);
                if (sport == null)
                {
                    TempData["ErrorMessage"] = "找不到此運動賽事";
                    return RedirectToAction("Index");
                }

                var zoneStats = await _dbContext.Seats
                    .Where(s => s.SportId == sportId)
                    .GroupBy(s => s.SeatZone)
                    .Select(g => new ZoneViewModel
                    {
                        ZoneName = g.Key,
                        TotalSeats = g.Count(),
                        AvailableSeats = g.Count(s => s.SeatStatus == 0),
                        SoldSeats = g.Count(s => s.SeatStatus == 1),
                        MinPrice = g.Min(s => s.Price),
                        MaxPrice = g.Max(s => s.Price)
                    })
                    .ToListAsync();

                ViewBag.Sport = sport;
                return View(zoneStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入區域選擇頁面失敗");
                TempData["ErrorMessage"] = "載入頁面時發生錯誤";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SelectQuantity(Guid sportId, string zone)
        {
            try
            {
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    TempData["ErrorMessage"] = "請先登入才能購票";
                    return RedirectToAction("Login", "Member");
                }

                var memberId = Guid.Parse(memberIdStr);
                var sport = await _dbContext.Sports.FindAsync(sportId);
                if (sport == null)
                {
                    TempData["ErrorMessage"] = "找不到此運動賽事";
                    return RedirectToAction("Index");
                }

                // ✅ 修正：改用 GetMemberTicketCountForEvent
                var currentTicketCount = await _orderService.GetMemberTicketCountForEvent(memberId, sportId, "sport");
                var remainingQuota = 4 - currentTicketCount;

                if (remainingQuota <= 0)
                {
                    TempData["ErrorMessage"] = "您已達到此場運動賽事的購票上限（4張）";
                    return RedirectToAction("SelectZone", new { sportId });
                }

                var zoneInfo = await _dbContext.Seats
                    .Where(s => s.SportId == sportId && s.SeatZone == zone)
                    .GroupBy(s => s.SeatZone)
                    .Select(g => new
                    {
                        ZoneName = g.Key,
                        AvailableSeats = g.Count(s => s.SeatStatus == 0),
                        Price = g.First().Price
                    })
                    .FirstOrDefaultAsync();

                if (zoneInfo == null || zoneInfo.AvailableSeats == 0)
                {
                    TempData["ErrorMessage"] = "此區域已售罄";
                    return RedirectToAction("SelectZone", new { sportId });
                }

                ViewBag.Sport = sport;
                ViewBag.Zone = zone;
                ViewBag.ZoneInfo = zoneInfo;
                ViewBag.RemainingQuota = remainingQuota;
                ViewBag.MaxQuantity = Math.Min(remainingQuota, Math.Min(4, zoneInfo.AvailableSeats));

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入數量選擇頁面失敗");
                TempData["ErrorMessage"] = "載入頁面時發生錯誤";
                return RedirectToAction("SelectZone", new { sportId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyCaptcha(Guid sportId, string zone, int quantity, string captchaInput, bool autoSelect = false)
        {
            try
            {
                var sessionCaptcha = HttpContext.Session.GetString("Captcha");

                if (string.IsNullOrEmpty(sessionCaptcha) ||
                    !sessionCaptcha.Equals(captchaInput, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "驗證碼錯誤，請重新輸入";
                    return RedirectToAction("SelectQuantity", new { sportId, zone });
                }

                HttpContext.Session.SetString("SelectedZone", zone);
                HttpContext.Session.SetInt32("SelectedQuantity", quantity);
                HttpContext.Session.Remove("Captcha");

                _logger.LogInformation($"驗證碼驗證成功，區域：{zone}，數量：{quantity}，自動選位：{autoSelect}");

                if (autoSelect)
                    return await AutoSelectSeats(sportId, zone, quantity);

                return RedirectToAction("SelectSeats", new { sportId, zone, quantity });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證碼驗證失敗");
                TempData["ErrorMessage"] = "驗證失敗，請重試";
                return RedirectToAction("SelectQuantity", new { sportId, zone });
            }
        }

        private async Task<IActionResult> AutoSelectSeats(Guid sportId, string zone, int quantity)
        {
            try
            {
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    TempData["ErrorMessage"] = "請先登入才能購票";
                    return RedirectToAction("Login", "Member");
                }

                var memberId = Guid.Parse(memberIdStr);

                var availableSeats = await _dbContext.Seats
                    .Where(s => s.SportId == sportId && s.SeatZone == zone && s.SeatStatus == 0)
                    .ToListAsync();

                if (availableSeats.Count < quantity)
                {
                    TempData["ErrorMessage"] = $"此區域可用座位不足（剩餘 {availableSeats.Count} 個）";
                    return RedirectToAction("SelectQuantity", new { sportId, zone });
                }

                var selectedSeats = SmartSeatSelection(availableSeats, quantity);

                if (selectedSeats == null || selectedSeats.Count != quantity)
                {
                    TempData["ErrorMessage"] = "系統選位失敗，請手動選位";
                    return RedirectToAction("SelectSeats", new { sportId, zone, quantity });
                }

                var seatIds = selectedSeats.Select(s => s.SeatId).ToList();

                // ✅ 修正：改用 CreateSportOrder
                var result = await _orderService.CreateSportOrder(memberId, sportId, seatIds);

                if (result.Success)
                {
                    HttpContext.Session.Remove("SelectedZone");
                    HttpContext.Session.Remove("SelectedQuantity");

                    var seatInfo = string.Join("、", selectedSeats.Select(s => $"{s.SeatRow}排{s.SeatNumber}號"));
                    _logger.LogInformation($"自動選位成功：{seatInfo}");
                    TempData["SuccessMessage"] = $"訂購成功！系統為您選擇：{zone} {seatInfo}";

                    return RedirectToAction("OrderDetail", "Member", new { orderId = result.OrderId });
                }
                else
                {
                    _logger.LogWarning($"自動選位訂單建立失敗：{result.Message}");
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("SelectQuantity", new { sportId, zone });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自動選位失敗");
                TempData["ErrorMessage"] = "自動選位失敗，請重試";
                return RedirectToAction("SelectQuantity", new { sportId, zone });
            }
        }

        private List<Seat> SmartSeatSelection(List<Seat> availableSeats, int quantity)
        {
            try
            {
                var sortedSeats = availableSeats
                    .OrderBy(s => int.TryParse(s.SeatRow, out int r) ? r : 999)
                    .ThenBy(s => s.SeatRow)
                    .ThenBy(s => int.TryParse(s.SeatNumber, out int n) ? n : 999)
                    .ThenBy(s => s.SeatNumber)
                    .ToList();

                if (quantity == 1)
                    return new List<Seat> { sortedSeats[0] };

                var consecutiveSeats = FindConsecutiveSeats(sortedSeats, quantity);
                if (consecutiveSeats != null && consecutiveSeats.Count == quantity)
                    return consecutiveSeats;

                return sortedSeats.Take(quantity).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "智能選位演算法執行失敗");
                return null;
            }
        }

        private List<Seat> FindConsecutiveSeats(List<Seat> sortedSeats, int quantity)
        {
            try
            {
                var seatsByRow = sortedSeats.GroupBy(s => s.SeatRow);

                foreach (var row in seatsByRow.OrderBy(g => int.TryParse(g.Key, out int r) ? r : 999))
                {
                    var rowSeats = row.OrderBy(s => int.TryParse(s.SeatNumber, out int n) ? n : 999).ToList();

                    for (int i = 0; i <= rowSeats.Count - quantity; i++)
                    {
                        var consecutiveGroup = new List<Seat> { rowSeats[i] };
                        bool isConsecutive = true;

                        for (int j = 1; j < quantity; j++)
                        {
                            if (int.TryParse(rowSeats[i + j - 1].SeatNumber, out int prevNum) &&
                                int.TryParse(rowSeats[i + j].SeatNumber, out int currNum) &&
                                currNum == prevNum + 1)
                            {
                                consecutiveGroup.Add(rowSeats[i + j]);
                            }
                            else
                            {
                                isConsecutive = false;
                                break;
                            }
                        }

                        if (isConsecutive && consecutiveGroup.Count == quantity)
                            return consecutiveGroup;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "尋找連號座位失敗");
                return null;
            }
        }

        private static Random _random = new Random();

        public IActionResult GenerateCaptcha(int chars = 4)
        {
            try
            {
                string captcha = GenerateCaptchaText(chars);
                HttpContext.Session.SetString("Captcha", captcha);

                int width = Math.Max(120, chars * 30);
                int height = 40;

                using var image = new Image<Rgba32>(width, height);
                image.Mutate(ctx => ctx.Fill(Color.White));

                var font = SystemFonts.CreateFont("Arial", 20, FontStyle.Bold);

                for (int i = 0; i < captcha.Length; i++)
                {
                    float x = 10 + i * 25;
                    float y = _random.Next(5, 15);
                    var textColor = Color.FromRgb(
                        (byte)_random.Next(0, 100),
                        (byte)_random.Next(0, 100),
                        (byte)_random.Next(100, 200));
                    image.Mutate(ctx => ctx.DrawText(captcha[i].ToString(), font, textColor, new PointF(x, y)));
                }

                for (int i = 0; i < 5; i++)
                {
                    image.Mutate(ctx => ctx.DrawLine(Color.Gray, 1,
                        new PointF(_random.Next(width), _random.Next(height)),
                        new PointF(_random.Next(width), _random.Next(height))));
                }

                for (int i = 0; i < 50; i++)
                    image[_random.Next(width), _random.Next(height)] = Color.Gray;

                using var ms = new MemoryStream();
                image.Save(ms, new PngEncoder());
                return File(ms.ToArray(), "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成驗證碼失敗");
                return BadRequest();
            }
        }

        private string GenerateCaptchaText(int chars = 4)
        {
            const string validChars = "23456789ABCDEFGHJKLMNPQRTUVWXYZ";
            var result = new char[chars];
            for (int i = 0; i < chars; i++)
                result[i] = validChars[_random.Next(validChars.Length)];
            return new string(result);
        }

        [HttpGet]
        public async Task<IActionResult> SelectSeats(Guid sportId, string? zone = null, int? quantity = null)
        {
            try
            {
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    TempData["ErrorMessage"] = "請先登入才能購票";
                    return RedirectToAction("Login", "Member");
                }

                var memberId = Guid.Parse(memberIdStr);
                var sessionZone = HttpContext.Session.GetString("SelectedZone");
                var sessionQuantity = HttpContext.Session.GetInt32("SelectedQuantity");

                if (string.IsNullOrEmpty(sessionZone) || !sessionQuantity.HasValue)
                {
                    TempData["ErrorMessage"] = "請重新選擇區域和張數";
                    return RedirectToAction("SelectZone", new { sportId });
                }

                var sport = await _dbContext.Sports.FindAsync(sportId);
                if (sport == null)
                {
                    TempData["ErrorMessage"] = "找不到此運動賽事";
                    return RedirectToAction("Index");
                }

                // ✅ 修正：改用 GetMemberTicketCountForEvent
                var currentTicketCount = await _orderService.GetMemberTicketCountForEvent(memberId, sportId, "sport");
                var remainingQuota = 4 - currentTicketCount;

                ViewBag.Sport = sport;
                ViewBag.CurrentTicketCount = currentTicketCount;
                ViewBag.RemainingQuota = remainingQuota;
                ViewBag.SelectedZone = sessionZone;
                ViewBag.SelectedQuantity = sessionQuantity;

                var seatsQuery = await _dbContext.Seats
                    .Where(s => s.SportId == sportId && s.SeatZone == sessionZone)
                    .ToListAsync();

                var seats = seatsQuery
                    .OrderBy(s => int.TryParse(s.SeatRow, out int r) ? r : 999)
                    .ThenBy(s => s.SeatRow)
                    .ThenBy(s => int.TryParse(s.SeatNumber, out int n) ? n : 999)
                    .ThenBy(s => s.SeatNumber)
                    .ToList();

                var memberSeatIds = await _dbContext.OrderDetails
                    .Where(od => od.Order.MemberId == memberId
                              && od.Order.SportId == sportId
                              && od.Order.OrderStatus != 2)
                    .Join(_dbContext.Seats,
                          od => od.OrderDetailId,
                          s => s.OrderDetailId,
                          (od, s) => s.SeatId)
                    .ToListAsync();

                ViewBag.MemberSeatIds = memberSeatIds;
                return View(seats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "選座頁面載入失敗");
                TempData["ErrorMessage"] = "載入頁面時發生錯誤";
                return RedirectToAction("SelectZone", new { sportId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] SportOrderRequest request)
        {
            try
            {
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                    return Json(new { success = false, message = "請先登入" });

                var memberId = Guid.Parse(memberIdStr);
                var sessionQuantity = HttpContext.Session.GetInt32("SelectedQuantity");

                if (request.SelectedSeats == null || !request.SelectedSeats.Any())
                    return Json(new { success = false, message = "請選擇至少一個座位" });

                if (sessionQuantity.HasValue && request.SelectedSeats.Count != sessionQuantity.Value)
                    return Json(new { success = false, message = $"請選擇 {sessionQuantity.Value} 個座位" });

                if (request.SelectedSeats.Count > 4)
                    return Json(new { success = false, message = "一次最多只能選擇 4 個座位" });

                // ✅ 修正：改用 CreateSportOrder
                var result = await _orderService.CreateSportOrder(memberId, request.SportId, request.SelectedSeats);

                if (result.Success)
                {
                    HttpContext.Session.Remove("SelectedZone");
                    HttpContext.Session.Remove("SelectedQuantity");
                    return Json(new { success = true, message = result.Message, orderId = result.OrderId });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立訂單時發生錯誤");
                return Json(new { success = false, message = "訂單建立失敗，請稍後再試" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckSeatStatus(Guid sportId, string? zone = null)
        {
            try
            {
                var query = _dbContext.Seats.Where(s => s.SportId == sportId);
                if (!string.IsNullOrEmpty(zone))
                    query = query.Where(s => s.SeatZone == zone);

                var seats = await query
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

    // 修正：改名避免與 ConcertController 的同名類別衝突
    public class SportOrderRequest
    {
        public Guid SportId { get; set; }
        public List<Guid> SelectedSeats { get; set; }
    }
}
