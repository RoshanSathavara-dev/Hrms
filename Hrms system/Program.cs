using Hrms_system.Data;
using Hrms_system.Models;
using Hrms_system.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;




var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ?? Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;               // No need for a number
    options.Password.RequireLowercase = false;           // No need for a lowercase letter
    options.Password.RequireUppercase = false;           // No need for an uppercase letter
    options.Password.RequireNonAlphanumeric = false;     // No need for a special character
    options.Password.RequiredLength = 6;                 // Minimum password length
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.AddScoped<IAttendancePolicyService, AttendancePolicyService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<LeaveAccrualService>();
builder.Services.AddHostedService<LeaveAccrualBackgroundService>();
// Program.cs
builder.Services.AddScoped<IAttendancePolicyService, AttendancePolicyService>();




builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8); // Auto logout after inactivity
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ Configure Authentication Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);  // shorter timeout
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    // 👇 Make cookie session-based
    options.Cookie.Expiration = null; // expire when browser closes
});





builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();



var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.InitializeAsync(services);
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication(); // ?? Enable Authentication
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.Run();