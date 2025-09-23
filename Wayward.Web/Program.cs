using Microsoft.EntityFrameworkCore;
using Wayward.Domain;
using Wayward.Domain.Identity;
using Wayward.Repository;
using Wayward.Service.Implementation;
using Wayward.Service.Interface;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
     options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();



builder.Services.AddDefaultIdentity<WaywardUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddTransient<IFlightService, FlightService>();
builder.Services.AddTransient<ISeatService, SeatService>();
builder.Services.AddTransient<IWishListService, WishListService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IBookingService, BookingService>();
builder.Services.AddTransient<IBoardingPassPdfService, BoardingPassPdfService>();


builder.Services.AddControllersWithViews().AddNewtonsoftJson(options =>
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);


builder.Services.AddHttpClient<IFetchService, FetchService>(client =>
{
    client.BaseAddress = new Uri("https://test.api.amadeus.com/");
});


builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
//builder.Services.AddSingleton<IPaymentService, StripePaymentService>();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Wayward.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
