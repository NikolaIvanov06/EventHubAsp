using CloudinaryDotNet;
using EventHubASP.Core;
using EventHubASP.Core.Hubs;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEmailService, EmailService>(provider =>
    new EmailService(
        builder.Configuration["EmailSettings:SmtpServer"],
        int.Parse(builder.Configuration["EmailSettings:SmtpPort"]),
        builder.Configuration["EmailSettings:SenderEmail"],
        builder.Configuration["EmailSettings:SenderPassword"]
    ));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var cloudinaryAccount = new Account(
    "dyzmm3onv",
    "448933911759921",
    "4CVDRLkMFY_-84HS4JHj29IQf7k"
);

var cloudinary = new Cloudinary(cloudinaryAccount);
builder.Services.AddSingleton(cloudinary);
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IRoleChangeRequestService, RoleChangeRequestService>();
builder.Services.AddScoped<RoleManagementService>();

builder.Services.AddDbContext<ApplicationDbContext>(b =>
    b.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("EventHubASP.DataAccess")));

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 0;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = false;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Organizer", policy => policy.RequireRole("Organizer"));
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            await IdentitySeeder.SeedRolesAndUsers(services);
            await EventSeeder.SeedEvents(services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during seeding: {ex.Message}");
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<NotificationHub>("/notificationHub");

app.Run();