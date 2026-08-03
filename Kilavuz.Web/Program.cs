using Kilavuz.Web.Application;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Data;
using Kilavuz.Web.Infrastructure.Captcha;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Konfigürasyonu
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// 2. Servislerin (IoC) Kaydedilmesi
// Generic Repository ve Service kayıtları
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped(typeof(IGenericService<>), typeof(GenericService<>));
builder.Services.AddScoped<ICaptchaProvider, AiGeneratedCaptchaProvider>();

// MVC ve View eklentileri
builder.Services.AddControllersWithViews();

// Oturum Yönetimi (Session) ve Cache
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = System.TimeSpan.FromMinutes(5); // 5 dakika
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. Kimlik Doğrulama (Cookie Auth)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Panel/Auth/Login";
        options.AccessDeniedPath = "/Panel/Auth/AccessDenied";
        options.Cookie.Name = "KilavuzAuthCookie";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = System.TimeSpan.FromDays(1);
    });

// 4. Yetkilendirme (Policy-based)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("YetkiliOrAdmin", policy => policy.RequireRole("SuperAdmin", "Yetkili"));
});

// 5. Rate Limiting (Güvenlik Kuralı 2.5)
var disableRateLimitInDev = builder.Configuration.GetValue<bool>("Security:DisableRateLimitInDev");
if (!disableRateLimitInDev || !builder.Environment.IsDevelopment())
{
    builder.Services.AddRateLimiter(options =>
    {
        // Login için kısıtlı politika
        options.AddFixedWindowLimiter("LoginPolicy", opt =>
        {
            opt.Window = System.TimeSpan.FromMinutes(1);
            opt.PermitLimit = 5;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0; // Login'de sıraya alma, direkt reddet
        });

        // Genel kullanım için politika
        options.AddFixedWindowLimiter("GlobalPolicy", opt =>
        {
            opt.Window = System.TimeSpan.FromMinutes(1);
            opt.PermitLimit = 100;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 2;
        });

        options.RejectionStatusCode = 429; // Too Many Requests
    });
}

var app = builder.Build();

// 5. Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // wwwroot altındaki INSPINIA dosyaları için

app.UseRouting();

// Rate Limiter Routing'den sonra, Auth'dan önce çağrılmalıdır
if (!disableRateLimitInDev || !builder.Environment.IsDevelopment())
{
    app.UseRateLimiter();
}

app.UseAuthentication(); // Önce kimlik doğrulama
app.UseAuthorization();  // Sonra yetki kontrolü
app.UseSession();        // Session aktif ediliyor

// Alan (Area) yönlendirmesi - Panel
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Varsayılan UI yönlendirmesi
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
