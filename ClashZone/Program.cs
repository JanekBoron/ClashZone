using ClashZone.DataAccess.DbInitializer;
using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services;
using ClashZone.Services.Interfaces;
using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ClashUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Rejestrujemy repozytoria wykorzystywane w aplikacji
builder.Services.AddScoped<ITournamentsRepository, TournamentsRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

//business
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IBracketService, BracketService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();

builder.Services.AddScoped<IProductsRepository, ProductsRepository>();
builder.Services.AddScoped<IProductRedeemRepository, ProductRedeemRepository>();
builder.Services.AddScoped<ICoinWalletRepository, CoinWalletRepository>();

builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<ICoinShopService, CoinShopService>();
builder.Services.AddScoped<ICoinWalletService, CoinWalletService>();

//Konfiguiracja Stripe do platnosci
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
/*    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = "swagger";
    });*/
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Needed if using wwwroot and static files (e.g., CSS, JS)

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await DbInitializer.InitializeAsync(app.Services);

app.Run();