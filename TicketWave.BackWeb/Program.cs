using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;
using TicketWave.Repository.Repositories.Interface;
using TicketWave.Repository.Repositories.Implement;
using TicketWave.Service.Services.Interface;
using TicketWave.Service.Services.Implement;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 資料庫連線
builder.Services.AddDbContext<MemberDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MemberDb")));

// 註冊 Repository
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
// 如果有其他 Repository，也在這裡註冊

// 註冊 Service
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IOrderService, OrderService>();
// 如果有其他 Service，也在這裡註冊

// Session 設定（後台管理員登入用）
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2); // 後台 Session 2 小時
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".TicketWave.BackWeb.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // 開發環境用 HTTP
});

// AutoMapper（如果需要）
// builder.Services.AddAutoMapper(typeof(Program));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // 啟用 Session


app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}"); // 預設到 Dashboard


app.Run();
