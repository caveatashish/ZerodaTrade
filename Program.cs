using Microsoft.EntityFrameworkCore;
using ZerodaTrade.Data;
using ZerodaTrade.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Enable Razor Pages
builder.Services.AddRazorPages();

// Add in-memory caching for script stats
builder.Services.AddMemoryCache();

// Add Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Initialize database
DbInitializer.Initialize(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=StockTrade}/{action=Index}/{id?}")
    .WithStaticAssets();

// Map Razor Pages
app.MapRazorPages();

app.Run();
