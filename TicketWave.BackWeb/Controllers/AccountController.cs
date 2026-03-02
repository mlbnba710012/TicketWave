using Microsoft.AspNetCore.Mvc;

namespace TicketWave.BackWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IConfiguration configuration, ILogger<AccountController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // 如果已登入，跳轉到 Dashboard
            var adminUsername = HttpContext.Session.GetString("AdminUsername");
            if (!string.IsNullOrEmpty(adminUsername))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password)
        {
            try
            {
                // 驗證輸入
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "請輸入帳號和密碼");
                    return View();
                }

                // 從 appsettings.json 讀取管理員帳密
                var adminUsername = _configuration["AdminAccount:Username"];
                var adminPassword = _configuration["AdminAccount:Password"];

                // 檢查設定是否存在
                if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
                {
                    _logger.LogError("appsettings.json 中缺少 AdminAccount 設定");
                    ModelState.AddModelError("", "系統設定錯誤，請聯絡管理員");
                    return View();
                }

                // 驗證帳號密碼
                if (username.Trim() == adminUsername && password.Trim() == adminPassword)
                {
                    // 登入成功，設定 Session
                    HttpContext.Session.SetString("AdminUsername", username);
                    HttpContext.Session.SetString("AdminLoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    _logger.LogInformation("管理員 {Username} 於 {Time} 登入成功", username, DateTime.Now);

                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    // 登入失敗
                    _logger.LogWarning("登入失敗：帳號 {Username}", username);
                    ModelState.AddModelError("", "帳號或密碼錯誤");
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登入處理時發生錯誤");
                ModelState.AddModelError("", "登入失敗，請稍後再試");
                return View();
            }
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            try
            {
                var username = HttpContext.Session.GetString("AdminUsername");

                // 清除 Session
                HttpContext.Session.Clear();

                _logger.LogInformation("管理員 {Username} 於 {Time} 登出", username ?? "未知", DateTime.Now);

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出時發生錯誤");
                return RedirectToAction("Login");
            }
        }
    }
}
