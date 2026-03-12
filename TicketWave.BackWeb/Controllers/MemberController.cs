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
        private readonly TicketWaveContext _dbContext;
        private readonly IMemberService _memberService;
        private readonly ILogger<MemberController> _logger;

        public MemberController(
            TicketWaveContext dbContext,
            IMemberService memberService,
            ILogger<MemberController> logger)
        {
            _dbContext = dbContext;
            _memberService = memberService;
            _logger = logger;
        }


        //檢查管理者登入
        private bool CheckAdminLogin()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername"));
        }

        /// <summary>
        /// 會員列表
        /// </summary>
        public async Task<IActionResult> Index(string searchTerm = "", bool showDeleted=false)
        {
            if (!CheckAdminLogin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var members = showDeleted ? await _memberService.GetAllIncludingDeleted() : await _memberService.GetAll();
                //var query = _dbContext.Members.AsQueryable();

                // 搜尋功能
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    members = members.Where(m=>(m.Name != null && m.Name.Contains(searchTerm))||
                    m.Email.Contains(searchTerm) || m.Phone.Contains(searchTerm) || m.NationalID.Contains(searchTerm)).ToList();



                    //query = query.Where(m =>
                    //    m.Name.Contains(searchTerm) ||
                    //    m.Email.Contains(searchTerm) ||
                    //    m.Phone.Contains(searchTerm) ||
                    //    m.NationalID.Contains(searchTerm));

                    ViewBag.SearchTerm = searchTerm;
                }

                //var members = await query
                //    .OrderByDescending(m => m.CreateDate)
                //    .ToListAsync();


                //按會員建立日期排序
                members = members.OrderByDescending(m => m.CreateDate).ToList();

                var allMembers = await _memberService.GetAllIncludingDeleted();

                ViewBag.ShowDeleted = showDeleted;
                ViewBag.TotalMembers = allMembers.Count;
                ViewBag.ActiveMembers = allMembers.Count(m => !m.IsDelete);
                ViewBag.DeletedMembers = allMembers.Count(m => m.IsDelete);

                return View(members);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢會員列表失敗");
                TempData["ErrorMessage"] = "查詢會員列表失敗，請稍後再試";

                ViewBag.ShowDeleted = showDeleted;
                ViewBag.TotalMembers = 0;
                ViewBag.ActiveMembers = 0;
                ViewBag.DeletedMembers = 0;

                return View(new List<Member>());
            }
        }

        /// <summary>
        /// 會員詳情
        /// </summary>
        public async Task<IActionResult> Details(Guid id)
        {
            if (!CheckAdminLogin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                //var member = await _dbContext.Members.FindAsync(id);
                var member = await _memberService.GetByIdIncludingDeleted(id);
                if (member == null)
                {
                    TempData["ErrorMessage"] = "找不到此會員資料";
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
                ViewBag.HasOrders = orders.Any();

                return View(member);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢會員詳情失敗");
                TempData["ErrorMessage"] = "查詢會員詳情失敗，請稍後再試";
                return NotFound();
            }
        }

        /// <summary>
        /// 軟刪除會員
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            if (!CheckAdminLogin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var result = await _memberService.SoftDeleteMember(id);

                if (result.Success) 
                {
                    TempData["SuccessMessage"] = "會員已軟刪除";
                    _logger.LogInformation($"管理員軟刪除會員 : MemberId={id}");
                }
                else
                {
                    TempData["ErrorMwssage"] = result.Message;
                }

                return RedirectToAction("Details", new { id });


                // 檢查是否有訂單
                //var hasOrders = await _dbContext.Orders.AnyAsync(o => o.MemberId == id);
                //if (hasOrders)
                //{
                //    TempData["ErrorMessage"] = "此會員有購票記錄，無法刪除";
                //    return RedirectToAction("Details", new { id });
                //}

                //var result = await _memberService.DeleteMember(id);
                //if (result)
                //{
                //    TempData["SuccessMessage"] = "會員刪除成功";
                //    return RedirectToAction("Index");
                //}
                //else
                //{
                //    TempData["ErrorMessage"] = "刪除失敗";
                //    return RedirectToAction("Details", new { id });
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"軟刪除會員失敗:MemberId={id}");
                TempData["ErrorMessage"] = "軟刪除會員失敗，請稍後再試";
                return RedirectToAction("Details", new { id });
            }
        }


        //恢復已刪除的會員帳號
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(Guid id)
        {
            if (!CheckAdminLogin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var result = await _memberService.RestoreMember(id);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "會員帳號已恢復，可以重新登入";
                    _logger.LogInformation($"管理員恢復會員帳號: MemberId={id}");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }

                return RedirectToAction("Details", new { id });
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"恢復會員失敗 : MemberId = {id}");
                TempData["ErrorMessage"] = "恢復會員失敗，請稍後再試";
                return RedirectToAction("Details", new { id });

            }

        }

        //永久刪除會員(需小心謹慎!)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentDelete(Guid id)
        {
            if (!CheckAdminLogin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var result = await _memberService.PermanentDeleteMember(id);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "會員帳號已永久刪除，無法恢復";
                    _logger.LogInformation($"管理員永久刪除會員 : MemberId={id}");
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Details", new { id });
                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"永久刪除會員失敗 : MemberId={id}");
                TempData["ErrorMessage"] = "永久刪除會員失敗，請稍後再試";
                return RedirectToAction("Details", new { id });

            }
        }

        //查看已刪除的會員列表
        public async Task<IActionResult> DeletedMembers()
        {
            if (!CheckAdminLogin())
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                var deletedMembers = await _memberService.GetDeletedMembers();
                ViewBag.TotalDeletedMembers = deletedMembers.Count;
                return View(deletedMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢已刪除會員列表失敗");
                TempData["ErrorMessage"] = "查詢已刪除會員列表失敗，請稍後再試";
                return View(new List<Member>());
            }

        }

    }
}
