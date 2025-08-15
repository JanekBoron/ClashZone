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

// Configure database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity.  Require email confirmation for login to enable account activation.
builder.Services.AddIdentity<ClashUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Register repositories used in the application
builder.Services.AddScoped<ITournamentsRepository, TournamentsRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IProductsRepository, ProductsRepository>();
builder.Services.AddScoped<IProductRedeemRepository, ProductRedeemRepository>();
builder.Services.AddScoped<ICoinWalletRepository, CoinWalletRepository>();

// Register business services
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IBracketService, BracketService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<ICoinShopService, CoinShopService>();
builder.Services.AddScoped<ICoinWalletService, CoinWalletService>();

// Register custom email service used for account activation and notifications
builder.Services.AddScoped<IEmailService, EmailService>();

// Stripe API configuration for payments (existing functionality)
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Swagger configuration can be enabled here when debugging
    // app.UseSwagger();
    // app.UseSwaggerUI(c =>
    // {
    //     c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    //     c.RoutePrefix = "swagger";
    // });
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Serve wwwroot content such as CSS and JS

app.UseRouting();

// Authentication must come before authorization to enable cookie auth and identity
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed the database if necessary
await DbInitializer.InitializeAsync(app.Services);

app.Run();