using Microsoft.EntityFrameworkCore;
using AutoMapper;
//using AutoMapper.Execution;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TicketWave.Repository.Entity;
using TicketWave.Service.Models.Info;
using TicketWave.Service.Services.Interface;
using TicketWave.Web.Models.Parameter;
using TicketWave.Service.Models.Info;

namespace TicketWave.Web.Controllers
{
    public class MemberController : Controller
    {
        private readonly IMemberService _memberService;
        private readonly ILogger<MemberController> _logger;
        private readonly IMapper _mapper;
        private readonly TicketWaveContext _dbContext;
        private readonly IOrderService _orderService;
        public MemberController(IMemberService memberService, ILogger<MemberController> logger, IMapper mapper, TicketWaveContext dbContext, IOrderService orderService)
        {
            _memberService = memberService;
            _logger = logger;
            _mapper = mapper;
            _dbContext = dbContext;
            _orderService = orderService;

        }

        public async Task<IActionResult> Index()
        {
            var result = await _memberService.GetAll();
            return View(result);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        //[ValidateAntiForgeryToken] // 加入安全性標籤
        public async Task<IActionResult> Register(RegisterParameter parameter)
        {
            //if (!ModelState.IsValid)
            //{
            //    return View();
            //}

            ////正常非陣列做法
            //var info = new RegisterInfo
            //{
            //    NationalID = parameter.NationalID,
            //    Phone = parameter.Phone,
            //    Email = parameter.Email,
            //    Password = parameter.Password

            //};

            ////陣列做法
            //var listInfo = new List<RegisterInfo>();
            //var info2 = new RegisterInfo();

            //foreach(var item in listInfo)
            //{
            //    info2 = new RegisterInfo();
            //    info2.NationalID = item.NationalID;
            //    info2.Phone = item.Phone;
            //    info2.Email = item.Email;
            //    info2.Password = item.Password;

            //    listInfo.Add(info2);
            //}
            //ModelState.Clear();

            if (!ModelState.IsValid)
            {
                return View(parameter);  // ← 返回 View 並顯示驗證錯誤
            }


            //var emailExists = await _memberService.GetByEmail(parameter.Email);
            //if (emailExists != null)
            //{
            //    ModelState.AddModelError("", "此 Email 已被使用");
            //    return View();
            //}

            var info = _mapper.Map<RegisterInfo>(parameter);
            var result = await _memberService.Register(info);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "註冊成功！請登入";
                //return RedirectToAction("Login");
                return View();
            }

            // 註冊失敗
            ModelState.AddModelError("", result.Message);
            return View(parameter);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            ModelState.Clear();
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "請輸入 Email 和密碼。");
                return View();
            }
            var result = await _memberService.Login(email, password);
            if (!result.Success)
            {
                //ModelState.AddModelError("", "登入失敗，請檢查您的 Email 和密碼。");
                ModelState.AddModelError("", result.Message);
                return View();
            }

            //var member = await _memberService.GetByEmail(email);
            //if (member != null)
            //{
            //    HttpContext.Session.SetString("MemberId", member.MemberId.ToString());
            //    HttpContext.Session.SetString("MemberName", member.Name ?? "會員");
            //    HttpContext.Session.SetString("MemberEmail", member.Email);
            //}

            if (result.Member != null)
            {
                HttpContext.Session.SetString("MemberId", result.Member.MemberId.ToString());
                HttpContext.Session.SetString("MemberName", result.Member.Name ?? "會員");
                HttpContext.Session.SetString("MemberEmail", result.Member.Email);
            }


            // 登錄成功，重定向到首頁或其他頁面
            return RedirectToAction("Index", "Home");
        }

        //public async Task<IActionResult> Logout()
        //{
        //    await _memberService.Logout();
        //    return RedirectToAction("Index", "Home");
        //}


        // <summary>
        // 個人資料頁面
        // </summary>
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // 檢查登入
            var memberIdStr = HttpContext.Session.GetString("MemberId");
            if (string.IsNullOrEmpty(memberIdStr))
            {
                return RedirectToAction("Login");
            }

            var memberId = Guid.Parse(memberIdStr);
            var member = await _memberService.GetById(memberId);

            if (member == null)
            {
                return RedirectToAction("Login");
            }

            return View(member);
        }

        //// <summary>
        //// 更新個人資料
        //// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Member member)
        {
            try
            {
                // 檢查登入
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    return RedirectToAction("Login");
                }

                var memberId = Guid.Parse(memberIdStr);

                // 確認是本人
                if (member.MemberId != memberId)
                {
                    TempData["ErrorMessage"] = "無權限修改此資料";
                    return RedirectToAction("Profile");
                }

                // 取得原始會員資料
                var existingMember = await _memberService.GetById(memberId);
                if (existingMember == null)
                {
                    return RedirectToAction("Login");
                }

                // 更新可修改的欄位
                existingMember.Name = member.Name;
                existingMember.Phone = member.Phone;
                existingMember.BirthDate = member.BirthDate;
                existingMember.Address = member.Address;
                existingMember.UpdateDate = DateTime.Now;

                // 保存變更
                var result = await _memberService.UpdateMemberProfile(new UpdateMemberProfileInfo
                {
                    MemberId = existingMember.MemberId,
                    Name = existingMember.Name,
                    Phone = existingMember.Phone,
                    BirthDate = existingMember.BirthDate,
                    Address = existingMember.Address
                });

                if (result)
                //if (result != null)
                {
                    //_dbContext.Update(existingMember);
                    //_dbContext.SaveChanges();

                    TempData["SuccessMessage"] = "個人資料更新成功！";

                    // 更新 Session 中的姓名
                    HttpContext.Session.SetString("MemberName", existingMember.Name ?? "會員");
                }
                else
                {
                    TempData["ErrorMessage"] = "個人資料更新失敗，請稍後再試";
                }

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新個人資料失敗");
                TempData["ErrorMessage"] = "更新失敗，請稍後再試";
                return RedirectToAction("Profile");
            }
        }

        /// <summary>
        /// 修改密碼頁面（GET）
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            // 檢查登入
            var memberIdStr = HttpContext.Session.GetString("MemberId");
            if (string.IsNullOrEmpty(memberIdStr))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        /// <summary>
        /// 修改密碼（POST）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordParameter parameter)
        {
            try
            {
                // 檢查登入
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    return RedirectToAction("Login");
                }

                var memberId = Guid.Parse(memberIdStr);

                // 檢查 ModelState
                if (!ModelState.IsValid)
                {
                    return View(parameter);
                }

                // 呼叫 Service 修改密碼
                var info = new ChangePasswordInfo
                {
                    MemberId = memberId,
                    OldPassword = parameter.OldPassword,
                    NewPassword = parameter.NewPassword
                };

                var result = await _memberService.ChangePassword(info);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    // 可以選擇：
                    // 1. 留在修改密碼頁面
                    return RedirectToAction("ChangePassword");
                    // 2. 或跳轉到個人資料頁面
                    // return RedirectToAction("Profile");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    return View(parameter);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改密碼失敗");
                TempData["ErrorMessage"] = "修改密碼時發生錯誤";
                return View(parameter);
            }
        }

        /// <summary>
        /// 刪除帳號（POST）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(string password)
        {
            try
            {
                // 檢查登入
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    return RedirectToAction("Login");
                }

                var memberId = Guid.Parse(memberIdStr);

                // 檢查密碼是否輸入
                if (string.IsNullOrEmpty(password))
                {
                    TempData["ErrorMessage"] = "請輸入密碼以確認刪除";
                    return RedirectToAction("ChangePassword");
                }

                // 呼叫 Service 刪除帳號
                var result = await _memberService.DeleteAccount(memberId, password);

                if (result.Success)
                {
                    // 清除 Session
                    HttpContext.Session.Clear();

                    TempData["SuccessMessage"] = "您的帳號已成功刪除";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("ChangePassword");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除帳號失敗");
                TempData["ErrorMessage"] = "刪除帳號時發生錯誤";
                return RedirectToAction("ChangePassword");
            }
        }


        /// <summary>
        /// 我的訂單列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            try
            {
                // 檢查登入
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    return RedirectToAction("Login");
                }

                var memberId = Guid.Parse(memberIdStr);

                // 查詢會員的所有訂單
                var orders = await _dbContext.Orders
                    .Where(o => o.MemberId == memberId)
                    .OrderByDescending(o => o.CreateDate)
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢訂單失敗");
                return View(new List<Order>());
            }
        }

        /// <summary>
        /// 訂單詳情
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OrderDetail(Guid orderId)
        {
            try
            {
                // 檢查登入
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    return RedirectToAction("Login");
                }

                var memberId = Guid.Parse(memberIdStr);

                // 查詢訂單
                var order = await _dbContext.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.MemberId == memberId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "找不到此訂單";
                    return RedirectToAction("Orders");
                }

                // 查詢訂單明細
                var orderDetails = await _dbContext.OrderDetails
                    .Where(od => od.OrderId == orderId)
                    .OrderBy(od => od.SeatZone)
                    .ThenBy(od => od.SeatRow)
                    .ThenBy(od => od.SeatNumber)
                    .ToListAsync();

                ViewBag.OrderDetails = orderDetails;

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢訂單詳情失敗");
                TempData["ErrorMessage"] = "查詢失敗，請稍後再試";
                return RedirectToAction("Orders");
            }
        }

        /// <summary>
        /// 取消訂單（刪除訂單）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(Guid orderId)
        {
            try
            {
                // 檢查登入
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    return Json(new { success = false, message = "請先登入" });
                }

                var memberId = Guid.Parse(memberIdStr);

                // 呼叫 OrderService 取消訂單
                var result = await _orderService.CancelOrder(orderId, memberId);

                if (result)
                {
                    TempData["SuccessMessage"] = "訂單已成功取消，座位已釋放";
                    //return Json(new { success = true, message = "訂單已取消" });
                    return RedirectToAction("Orders");
                }
                else
                {
                    TempData["ErrorMessage"] = "取消訂單失敗，此訂單可能不存在或已付款";
                    return Json(new { success = false, message = "取消失敗" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消訂單失敗");
                return Json(new { success = false, message = "取消訂單時發生錯誤" });
            }
        }

        /// <summary>
        /// 重新選座（取消訂單並跳轉到選座頁面）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReSelectSeats(Guid orderId)
        {
            try
            {
                // 檢查登入
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    TempData["ErrorMessage"] = "請先登入";
                    return RedirectToAction("Login");
                }

                var memberId = Guid.Parse(memberIdStr);

                // 先查詢訂單資訊
                var order = await _dbContext.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.MemberId == memberId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "找不到此訂單";
                    return RedirectToAction("Orders");
                }

                // 只能重新選座待付款的訂單
                if (order.OrderStatus != 0)
                {
                    TempData["ErrorMessage"] = "只有待付款的訂單才能重新選座";
                    return RedirectToAction("Orders");
                }

                var concertId = order.ConcertId;

                // 取消訂單（釋放座位）
                var cancelResult = await _orderService.CancelOrder(orderId, memberId);

                if (!cancelResult)
                {
                    TempData["ErrorMessage"] = "取消訂單失敗";
                    return RedirectToAction("Orders");
                }

                // 跳轉到選座頁面
                TempData["InfoMessage"] = "原訂單已取消，請重新選擇座位";
                return RedirectToAction("SelectSeats", "Concert", new { concertId = concertId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新選座失敗");
                TempData["ErrorMessage"] = "重新選座時發生錯誤";
                return RedirectToAction("Orders");
            }
        }

        /// <summary>
        /// 新增選座（保留原訂單，繼續選座）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSeats(Guid orderId)
        {
            try
            {
                // 檢查是否登入
                var memberIdStr = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdStr))
                {
                    TempData["ErrorMessage"] = "請先登入";
                    return RedirectToAction("Login");
                }

                var memberId = Guid.Parse(memberIdStr);

                // 查詢訂單
                var order = await _dbContext.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.MemberId == memberId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "找不到訂單";
                    return RedirectToAction("Orders");
                }

                // 檢查訂單狀態（只有待付款才能新增選座）
                if (order.OrderStatus != 0)
                {
                    TempData["ErrorMessage"] = "此訂單狀態無法新增選座";
                    return RedirectToAction("Orders");
                }

                // 檢查該會員在這場演唱會已購買的總票數
                var currentTicketCount = await _orderService.GetMemberTicketCountForConcert(memberId, order.ConcertId);

                // 檢查是否已達上限（4張）
                if (currentTicketCount >= 4)
                {
                    TempData["ErrorMessage"] = "您已達到此場演唱會的購票上限（4張）";
                    return RedirectToAction("Orders");
                }

                // 計算剩餘可購買數量
                var remainingQuota = 4 - currentTicketCount;

                // 查詢演唱會資訊
                var concert = await _dbContext.Concerts.FindAsync(order.ConcertId);
                if (concert == null)
                {
                    TempData["ErrorMessage"] = "找不到演唱會資訊";
                    return RedirectToAction("Orders");
                }

                // 儲存訂單 ID 到 Session（用於後續新增座位）
                HttpContext.Session.SetString("AddToOrderId", orderId.ToString());
                HttpContext.Session.SetInt32("RemainingQuota", remainingQuota);

                // 導向區域選擇頁面
                return RedirectToAction("SelectZone", "Concert", new { concertId = order.ConcertId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增選座失敗");
                TempData["ErrorMessage"] = "操作失敗，請稍後再試";
                return RedirectToAction("Orders");
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }


        public async Task<IActionResult> UpdateMemberProfile(UpdateMemberProfileParameter parameter)
        {
            var info = _mapper.Map<UpdateMemberProfileInfo>(parameter);
            var member = await _memberService.UpdateMemberProfile(info);

            return View();
        }

        public async Task<IActionResult> DeleteMember(Guid memberId)
        {
            var member = await _memberService.GetById(memberId);
            if (member == null) return NotFound();
            return View(member);
        }
    }
}
