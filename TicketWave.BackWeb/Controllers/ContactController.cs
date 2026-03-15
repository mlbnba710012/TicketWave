using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;

namespace TicketWave.BackWeb.Controllers
{
    public class ContactController : Controller
    {
        private readonly TicketWaveContext _dbContext;
        private readonly ILogger<ContactController> _logger;

        public ContactController(TicketWaveContext dbContext, ILogger<ContactController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        private bool CheckAdminLogin() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername"));

        // 留言列表
        public async Task<IActionResult> Index(int? status = null, string category = "")
        {
            if (!CheckAdminLogin()) return RedirectToAction("Login", "Account");

            try
            {
                var query = _dbContext.ContactMessages.AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(m => m.Status == status.Value);
                    ViewBag.SelectedStatus = status.Value;
                }
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(m => m.Category == category);
                    ViewBag.SelectedCategory = category;
                }

                var messages = await query
                    .OrderByDescending(m => m.CreateDate)
                    .ToListAsync();

                ViewBag.TotalCount      = messages.Count;
                ViewBag.PendingCount    = messages.Count(m => m.Status == 0);
                ViewBag.ProcessingCount = messages.Count(m => m.Status == 1);
                ViewBag.RepliedCount    = messages.Count(m => m.Status == 2);

                return View(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取聯絡留言列表失敗");
                TempData["ErrorMessage"] = "讀取留言列表失敗";
                return View(new List<ContactMessage>());
            }
        }

        // 留言詳情 + 回覆
        public async Task<IActionResult> Details(Guid id)
        {
            if (!CheckAdminLogin()) return RedirectToAction("Login", "Account");

            var message = await _dbContext.ContactMessages.FindAsync(id);
            if (message == null)
            {
                TempData["ErrorMessage"] = "找不到此留言";
                return RedirectToAction("Index");
            }

            // 標記為處理中
            if (message.Status == 0)
            {
                message.Status = 1;
                message.UpdateDate = DateTime.Now;
                await _dbContext.SaveChangesAsync();
            }

            return View(message);
        }

        // 回覆留言
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(Guid id, string adminReply)
        {
            if (!CheckAdminLogin()) return RedirectToAction("Login", "Account");

            try
            {
                var message = await _dbContext.ContactMessages.FindAsync(id);
                if (message == null)
                {
                    TempData["ErrorMessage"] = "找不到此留言";
                    return RedirectToAction("Index");
                }

                message.AdminReply = adminReply?.Trim();
                message.Status     = 2; // 已回覆
                message.UpdateDate = DateTime.Now;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("管理員回覆留言：{MessageId}", id);
                TempData["SuccessMessage"] = "回覆成功";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "回覆留言失敗");
                TempData["ErrorMessage"] = "回覆失敗，請稍後再試";
                return RedirectToAction("Details", new { id });
            }
        }

        // 更新狀態
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid id, int status)
        {
            if (!CheckAdminLogin()) return RedirectToAction("Login", "Account");

            var message = await _dbContext.ContactMessages.FindAsync(id);
            if (message != null)
            {
                message.Status = status;
                message.UpdateDate = DateTime.Now;
                await _dbContext.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id });
        }
    }
}
