using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;
using TicketWave.Repository.Repositories.Implement;
using TicketWave.Repository.Repositories.Interface;
using TicketWave.Service.Services.Implement;
using TicketWave.Service.Services.Interface;
using TicketWave.Web.Extensions;
using TicketWave.Web.Profiles;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MemberDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// 註冊 service and repository
builder.Services.AddFeatureServices();

// ? 正確的 Session 設定（修復 Cookie 問題）
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".TicketWave.Session";  // 明確設定 Cookie 名稱
    options.Cookie.SameSite = SameSiteMode.Lax;   // 允許同站請求
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;  // HTTP 環境（開發用）
});

builder.Services.AddAutoMapper(typeof(TicketWaveProfile).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ? 暫時註解掉 HTTPS 重定向（因為使用 HTTP）
// app.UseHttpsRedirection();

// ? 正確的中介軟體順序
app.UseStaticFiles();

app.UseRouting();

// ? Session 必須在 UseRouting 之後，UseAuthorization 之前
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
