using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using System.Globalization;
using Dapper;
using TestOtomasyon.Helpers;
using TestOtomasyon.Repositories;
using TestOtomasyon.Repositories.Interfaces;
using TestOtomasyon.Resources.Languages;
using TestOtomasyon.Services;


var builder = WebApplication.CreateBuilder(args);
DapperConfig.Configure();

// ResourcesPath boş bırakıldı: marker sınıfı (Lang) zaten Resources.Languages namespace'inde,
// .NET bu namespace'i klasör olarak çözüyor. ResourcesPath="Resources" verildiğinde
// Resources/Resources/Languages/... şeklinde yanlış yola bakıyordu.
builder.Services.AddLocalization();
builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(Lang));
    });

builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("tr-TR"),
        new CultureInfo("en-US")
    };

    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr-TR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // Tarayıcının dil tercihi varsayılanı ezmesin — sadece kullanıcı butona tıklayınca dil değişsin
    var acceptLangProvider = options.RequestCultureProviders
        .FirstOrDefault(p => p is Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider);
    if (acceptLangProvider != null)
        options.RequestCultureProviders.Remove(acceptLangProvider);
});

// Logging ekle
builder.Services.AddLogging(config =>
{
    config.AddDebug();
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "TestOtomasyonAuth";
    });

builder.Services.AddAuthorization(options =>
{

    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});


builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);
        opt.PermitLimit = 10;
        opt.QueueLimit = 2;
    });
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IAuthorityRepository, AuthorityRepository>();
builder.Services.AddScoped<IWorkTaskRepository, WorkTaskRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();



var app = builder.Build();

LocalizationAccessor.Configure(app.Services);


using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    try
    {
        using var conn = dbFactory.CreateConnection();
        conn.Open();
        var checkSql = "SELECT COUNT(*) FROM sys.tables WHERE name = 'Message'";
        var exists = conn.ExecuteScalar<int>(checkSql);
        if (exists == 0)
        {
            var createSql = @"
                CREATE TABLE [Message] (
                    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                    senderId UNIQUEIDENTIFIER NOT NULL,
                    recipientId UNIQUEIDENTIFIER NOT NULL,
                    subject NVARCHAR(300) NOT NULL,
                    body NVARCHAR(MAX) NULL,
                    sentAt DATETIME NOT NULL DEFAULT GETDATE(),
                    isRead BIT NOT NULL DEFAULT 0,
                    status INT NOT NULL DEFAULT 1
                )";
            conn.Execute(createSql);
            Console.WriteLine("Message tablosu oluşturuldu!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Message tablosu kontrolü hatası: {ex.Message}");
    }

    // Demo kullanıcı seed (idempotent: ilk açılışta çalışır, sonra atlar)
    try
    {
        await DatabaseSeeder.SeedAsync(dbFactory);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Demo kullanıcı seed hatası: {ex.Message}");
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var locOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();