using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;
using TicketWave.Service.Services.Interface;

namespace TicketWave.BackWeb.Controllers
{
    /// <summary>
    /// 會員管理
    /// </summary>
    public class MemberController : Controller
    {
        private readonly MemberDbContext _dbContext;
        private readonly IMemberService _memberService;
        private readonly ILogger<MemberController> _logger;

        public MemberController(
            MemberDbContext dbContext,
            IMemberService memberService,
            ILogger<MemberController> logger)
        {
            _dbContext = dbContext;
            _memberService = memberService;
            _logger = logger;
        }

        /// <summary>
        /// 會員列表
        /// </summary>
        public async Task<IActionResult> Index(string searchTerm = "")
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var query = _dbContext.Members.AsQueryable();

                // 搜尋功能
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(m =>
                        m.Name.Contains(searchTerm) ||
                        m.Email.Contains(searchTerm) ||
                        m.Phone.Contains(searchTerm) ||
                        m.NationalID.Contains(searchTerm));

                    ViewBag.SearchTerm = searchTerm;
                }

                var members = await query
                    .OrderByDescending(m => m.CreateDate)
                    .ToListAsync();

                return View(members);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢會員列表失敗");
                return View(new List<Member>());
            }
        }

        /// <summary>
        /// 會員詳情
        /// </summary>
        public async Task<IActionResult> Details(Guid id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var member = await _dbContext.Members.FindAsync(id);
                if (member == null)
                {
                    return NotFound();
                }

                // 查詢會員的訂單
                var orders = await _dbContext.Orders
                    .Where(o => o.MemberId == id)
                    .OrderByDescending(o => o.CreateDate)
                    .ToListAsync();

                ViewBag.Orders = orders;
                ViewBag.TotalOrders = orders.Count;
                ViewBag.TotalSpent = orders.Where(o => o.OrderStatus == 1).Sum(o => o.TotalAmount);

                return View(member);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢會員詳情失敗");
                return NotFound();
            }
        }

        /// <summary>
        /// 刪除會員（需謹慎）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 檢查是否有訂單
                var hasOrders = await _dbContext.Orders.AnyAsync(o => o.MemberId == id);
                if (hasOrders)
                {
                    TempData["ErrorMessage"] = "此會員有購票記錄，無法刪除";
                    return RedirectToAction("Details", new { id });
                }

                var result = await _memberService.DeleteMember(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "會員刪除成功";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "刪除失敗";
                    return RedirectToAction("Details", new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除會員失敗");
                TempData["ErrorMessage"] = "刪除失敗，請稍後再試";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}
