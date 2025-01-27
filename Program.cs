using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using UTB.OfficeCalendar.Application.Abstraction;
using UTB.OfficeCalendar.Application.Implementation;
using UTB.OfficeCalendar.Infrastructure.Database;
using UTB.OfficeCalendar.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
//builder.Password = builder.Configuration["FAIPASS"];
string connectionString = builder.Configuration.GetConnectionString("MySQL");
ServerVersion serverVersion = new MySqlServerVersion("8.0.40");
builder.Services.AddDbContext<CalendarDbContext>(OptionsBuilder => OptionsBuilder.UseMySql(connectionString, serverVersion));

builder.Services.AddIdentity<User, Role>()
     .AddEntityFrameworkStores<CalendarDbContext>()
     .AddDefaultTokenProviders();


builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequiredUniqueChars = 1;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 10;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.User.RequireUniqueEmail = true;
});


builder.Services.AddScoped<IAccountService, AccountIdentityService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>(serviceProvider => new FileUploadService(serviceProvider.GetService<IWebHostEnvironment>().WebRootPath));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization(); 


app.UseEndpoints(endpoints =>
{
    // Area route
    endpoints.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    // Default route
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});


app.Run();
