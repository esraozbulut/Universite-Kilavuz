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
using Kilavuz.Web.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Konfigürasyonu
builder.Services.AddHttpContextAccessor();
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Filter.With(new Kilavuz.Web.Infrastructure.Logging.AuditLogFilter(
        services.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
        context.Configuration))
);

// 2. Servislerin (IoC) Kaydedilmesi
// Generic Repository ve Service kayıtları
builder.Services.AddSingleton<IDbConnectionFactory, Kilavuz.Web.Data.SqlConnectionFactory>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped(typeof(IGenericService<>), typeof(GenericService<>));
builder.Services.AddScoped(typeof(Kilavuz.Web.Application.Interfaces.IResourceOwnershipPolicy<>), typeof(Kilavuz.Web.Application.ResourceOwnershipPolicy<>));
builder.Services.AddScoped(typeof(Kilavuz.Web.Application.Interfaces.IReorderService<>), typeof(Kilavuz.Web.Application.ReorderService<>));
builder.Services.AddScoped<ICaptchaProvider, AiGeneratedCaptchaProvider>();
builder.Services.AddScoped<IErrorLogService, ErrorLogService>();
builder.Services.AddScoped<IFileStorageService, Kilavuz.Web.Infrastructure.Storage.FileStorageService>();
builder.Services.AddScoped<IHtmlSanitizerService, Kilavuz.Web.Infrastructure.Security.HtmlSanitizerService>();
builder.Services.AddScoped<IPageService, Kilavuz.Web.Application.Services.PageService>();
builder.Services.AddScoped<IAuthenticationProvider, Kilavuz.Web.Infrastructure.Security.LocalTestAuthProvider>();
builder.Services.AddExceptionHandler<Kilavuz.Web.Infrastructure.Middleware.GlobalExceptionHandler>();

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
        options.Cookie.Name = "Kilavuz.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        options.ExpireTimeSpan = System.TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// 4. Yetkilendirme (Policy-based)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("YetkiliOrAbove", policy => policy.RequireRole("SuperAdmin", "Yetkili"));
});

// 5. Rate Limiting (Güvenlik Kuralı 2.5)
var disableRateLimitInDev = builder.Configuration.GetValue<bool>("Security:DisableRateLimitInDev");
if (!disableRateLimitInDev || !builder.Environment.IsDevelopment())
{
    builder.Services.AddRateLimiter(options =>
    {
        // Login için kısıtlı politika (Username + IP bazlı)
        options.AddPolicy<string, Kilavuz.Web.Infrastructure.Security.LoginRateLimiterPolicy>("LoginPolicy");

        // Genel kullanım için politika (IP bazlı)
        options.AddPolicy("GlobalPolicy", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    QueueLimit = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    Window = TimeSpan.FromMinutes(1)
                }));

        options.RejectionStatusCode = 429; // Too Many Requests
    });
}

var app = builder.Build();

// 5. Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(_ => { }); // This allows IExceptionHandler to run in Dev without a specific fallback route
}
else
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
    // Form data asenkron okumak için küçük bir middleware (LoginRateLimiterPolicy senkron çalıştığı için)
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/Panel/Auth/Login" && HttpMethods.IsPost(context.Request.Method) && context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync();
            if (form.TryGetValue("Username", out var usernameValue))
            {
                context.Items["LoginUsername"] = usernameValue.ToString();
            }
        }
        await next();
    });

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
