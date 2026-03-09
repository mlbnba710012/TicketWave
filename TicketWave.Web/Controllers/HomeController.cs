using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;
using TicketWave.Web.Models;

namespace TicketWave.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly TicketWaveContext _dbContext;

        public HomeController(TicketWaveContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 演唱會列表頁面
        /// </summary>
        public async Task<IActionResult> Concerts()
        {
            try
            {
                // 查詢所有售票中的演唱會，按日期排序
                var concerts = await _dbContext.Concerts
                    .Where(c => c.Status == 1) // 只顯示售票中的演唱會
                    .OrderBy(c => c.PerformanceDate)
                    .ToListAsync();

                return View(concerts);
            }
            catch (Exception ex)
            {
                // 記錄錯誤
                Console.WriteLine($"Error loading concerts: {ex.Message}");

                // 返回空列表
                return View(new List<Concert>());
            }
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Ticket()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
