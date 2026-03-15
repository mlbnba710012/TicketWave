using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TicketWave.Repository.Entity;
using ClosedXML.Excel;

namespace TicketWave.BackWeb.Controllers
{
    public class ReportController : Controller
    {
        private readonly TicketWaveContext _dbContext;
        private readonly ILogger<ReportController> _logger;

        public ReportController(TicketWaveContext dbContext, ILogger<ReportController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        private bool CheckAdminLogin() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername"));

        // 銷售報表主頁
        public async Task<IActionResult> Index(
            string eventType = "concert",
            Guid? eventId = null)
        {
            if (!CheckAdminLogin()) return RedirectToAction("Login", "Account");

            try
            {
                ViewBag.EventType = eventType;

                // 載入所有活動供下拉選單
                ViewBag.Concerts = await _dbContext.Concerts
                    .OrderByDescending(c => c.PerformanceDate).ToListAsync();
                ViewBag.Sports = await _dbContext.Sports
                    .OrderByDescending(s => s.PerformanceDate).ToListAsync();
                ViewBag.Theaters = await _dbContext.Theaters
                    .OrderByDescending(t => t.PerformanceDate).ToListAsync();

                if (eventId == null) return View();

                ViewBag.EventId = eventId;

                // 查詢選定活動的銷售資料
                var report = await BuildReport(eventType, eventId.Value);
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "產生銷售報表失敗");
                TempData["ErrorMessage"] = "產生報表失敗";
                return View();
            }
        }

        // 下載 CSV
        public async Task<IActionResult> DownloadCsv(string eventType, Guid eventId)
        {
            if (!CheckAdminLogin()) return RedirectToAction("Login", "Account");

            var report = await BuildReport(eventType, eventId);
            if (report == null) return NotFound();

            var sb = new StringBuilder();
            sb.AppendLine("區域,總座位數,已售出,剩餘,售出率(%),票價(NT$),銷售金額(NT$)");
            foreach (var row in report.ZoneRows)
            {
                sb.AppendLine($"{row.Zone},{row.TotalSeats},{row.SoldSeats},{row.AvailableSeats}," +
                              $"{row.SoldPercentage:F1},{row.Price:N0},{row.Revenue:N0}");
            }
            sb.AppendLine($"合計,{report.TotalSeats},{report.TotalSold},{report.TotalAvailable}," +
                          $"{report.TotalSoldPercentage:F1},,{report.TotalRevenue:N0}");

            var fileName = $"SalesReport_{report.EventName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv";
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", fileName);
        }

        // 下載 Excel
        public async Task<IActionResult> DownloadExcel(string eventType, Guid eventId)
        {
            if (!CheckAdminLogin()) return RedirectToAction("Login", "Account");

            var report = await BuildReport(eventType, eventId);
            if (report == null) return NotFound();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("銷售報表");

            // 標題
            ws.Cell(1, 1).Value = $"TicketWave 銷售報表 - {report.EventName}";
            ws.Range(1, 1, 1, 7).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1a73e8"))
                .Font.SetFontColor(XLColor.White);

            ws.Cell(2, 1).Value = $"產出時間：{DateTime.Now:yyyy/MM/dd HH:mm}";
            ws.Range(2, 1, 2, 7).Merge().Style.Font.SetItalic(true).Font.SetFontColor(XLColor.Gray);

            // 表頭
            var headers = new[] { "區域", "總座位數", "已售出", "剩餘", "售出率(%)", "票價(NT$)", "銷售金額(NT$)" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(4, i + 1).Value = headers[i];
            }
            ws.Range(4, 1, 4, 7).Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#e8f0fe"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // 資料列
            int row = 5;
            foreach (var r in report.ZoneRows)
            {
                ws.Cell(row, 1).Value = r.Zone;
                ws.Cell(row, 2).Value = r.TotalSeats;
                ws.Cell(row, 3).Value = r.SoldSeats;
                ws.Cell(row, 4).Value = r.AvailableSeats;
                ws.Cell(row, 5).Value = $"{r.SoldPercentage:F1}%";
                ws.Cell(row, 6).Value = r.Price;
                ws.Cell(row, 7).Value = r.Revenue;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";

                // 售出率高亮
                if (r.SoldPercentage >= 80)
                    ws.Row(row).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#d4edda"));

                row++;
            }

            // 合計列
            ws.Cell(row, 1).Value = "合計";
            ws.Cell(row, 2).Value = report.TotalSeats;
            ws.Cell(row, 3).Value = report.TotalSold;
            ws.Cell(row, 4).Value = report.TotalAvailable;
            ws.Cell(row, 5).Value = $"{report.TotalSoldPercentage:F1}%";
            ws.Cell(row, 7).Value = report.TotalRevenue;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
            ws.Range(row, 1, row, 7).Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1a73e8"))
                .Font.SetFontColor(XLColor.White);

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Position = 0;

            var fileName = $"SalesReport_{report.EventName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // 共用：建立報表資料
        private async Task<SalesReport> BuildReport(string eventType, Guid eventId)
        {
            string eventName = "";
            List<SalesZoneRow> zoneRows;

            // 查詢座位資料依區域彙總
            IQueryable<Seat> seatsQuery = eventType switch
            {
                "sport"   => _dbContext.Seats.Where(s => s.SportId == eventId),
                "theater" => _dbContext.Seats.Where(s => s.TheaterId == eventId),
                _         => _dbContext.Seats.Where(s => s.ConcertId == eventId)
            };

            // 取得活動名稱
            eventName = eventType switch
            {
                "sport"   => (await _dbContext.Sports.FindAsync(eventId))?.SportName ?? "",
                "theater" => (await _dbContext.Theaters.FindAsync(eventId))?.TheaterName ?? "",
                _         => (await _dbContext.Concerts.FindAsync(eventId))?.ConcertName ?? ""
            };

            var seats = await seatsQuery.ToListAsync();

            zoneRows = seats
                .GroupBy(s => new { s.SeatZone, s.Price })
                .Select(g => new SalesZoneRow
                {
                    Zone           = g.Key.SeatZone,
                    Price          = g.Key.Price,
                    TotalSeats     = g.Count(),
                    SoldSeats      = g.Count(s => s.SeatStatus == 1),
                    AvailableSeats = g.Count(s => s.SeatStatus == 0),
                })
                .OrderBy(r => r.Zone)
                .ToList();

            foreach (var r in zoneRows)
            {
                r.Revenue          = r.SoldSeats * r.Price;
                r.SoldPercentage   = r.TotalSeats > 0 ? (r.SoldSeats * 100.0 / r.TotalSeats) : 0;
            }

            // 也查訂單統計
            IQueryable<Order> ordersQuery = eventType switch
            {
                "sport"   => _dbContext.Orders.Where(o => o.SportId == eventId),
                "theater" => _dbContext.Orders.Where(o => o.TheaterId == eventId),
                _         => _dbContext.Orders.Where(o => o.ConcertId == eventId)
            };
            var orders = await ordersQuery.ToListAsync();

            return new SalesReport
            {
                EventName            = eventName,
                EventType            = eventType,
                EventId              = eventId,
                ZoneRows             = zoneRows,
                TotalSeats           = zoneRows.Sum(r => r.TotalSeats),
                TotalSold            = zoneRows.Sum(r => r.SoldSeats),
                TotalAvailable       = zoneRows.Sum(r => r.AvailableSeats),
                TotalRevenue         = zoneRows.Sum(r => r.Revenue),
                TotalOrders          = orders.Count,
                PaidOrders           = orders.Count(o => o.OrderStatus == 1),
                CancelledOrders      = orders.Count(o => o.OrderStatus == 2),
                ReportGeneratedAt    = DateTime.Now
            };
        }
    }

    // ViewModel
    public class SalesZoneRow
    {
        public string Zone           { get; set; }
        public decimal Price         { get; set; }
        public int TotalSeats        { get; set; }
        public int SoldSeats         { get; set; }
        public int AvailableSeats    { get; set; }
        public decimal Revenue       { get; set; }
        public double SoldPercentage { get; set; }
    }

    public class SalesReport
    {
        public string EventName          { get; set; }
        public string EventType          { get; set; }
        public Guid EventId              { get; set; }
        public List<SalesZoneRow> ZoneRows { get; set; }
        public int TotalSeats            { get; set; }
        public int TotalSold             { get; set; }
        public int TotalAvailable        { get; set; }
        public decimal TotalRevenue      { get; set; }
        public int TotalOrders           { get; set; }
        public int PaidOrders            { get; set; }
        public int CancelledOrders       { get; set; }
        public double TotalSoldPercentage =>
            TotalSeats > 0 ? (TotalSold * 100.0 / TotalSeats) : 0;
        public DateTime ReportGeneratedAt { get; set; }
    }
}
