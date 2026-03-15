using Microsoft.AspNetCore.Mvc;
using TicketWave.Repository.Entity;
using TicketWave.Web.Services;

namespace TicketWave.Web.Controllers
{
    public class ContactController : Controller
    {
        private readonly TicketWaveContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            TicketWaveContext dbContext,
            IEmailService emailService,
            ILogger<ContactController> logger)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // 如果已登入，預填姓名和 Email
            var memberIdStr = HttpContext.Session.GetString("MemberId");
            if (!string.IsNullOrEmpty(memberIdStr))
            {
                var memberId = Guid.Parse(memberIdStr);
                var member = _dbContext.Members.Find(memberId);
                if (member != null)
                {
                    ViewBag.PrefilledName  = member.Name;
                    ViewBag.PrefilledEmail = member.Email;
                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            string name, string email, string subject,
            string category, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
                {
                    TempData["ErrorMessage"] = "請填寫所有必填欄位";
                    return View();
                }

                // 儲存到資料庫
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                var contact = new ContactMessage
                {
                    MessageId  = Guid.NewGuid(),
                    MemberId   = string.IsNullOrEmpty(memberIdStr) ? null : Guid.Parse(memberIdStr),
                    Name       = name.Trim(),
                    Email      = email.Trim(),
                    Subject    = subject.Trim(),
                    Category   = category ?? "其他",
                    Message    = message.Trim(),
                    Status     = 0,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                };

                _dbContext.ContactMessages.Add(contact);
                await _dbContext.SaveChangesAsync();

                // 寄送 Email 通知給管理者（非同步，不影響用戶體驗）
                _ = _emailService.SendContactNotificationAsync(
                    contact.Name, contact.Email,
                    contact.Subject, contact.Category, contact.Message);

                _logger.LogInformation("新聯絡留言：{Name} {Email}", name, email);
                TempData["SuccessMessage"] = "您的留言已送出，我們將盡快回覆！";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "儲存聯絡留言失敗");
                TempData["ErrorMessage"] = "留言送出失敗，請稍後再試";
                return View();
            }
        }
    }
}
