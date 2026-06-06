using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Ports.Games;
using ClashZone.App.Services;
using ClashZone.App.Services.Games.Riot;
using ClashZone.App.Services.Interfaces;
using ClashZone.DataAccess.DbInitializer;
using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository;
using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(cfg => cfg.LicenseKey = "",
    typeof(Program),
    typeof(ClashZone.DataAccess.Config.ClashUserProfile));

// Configure database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity.  Require email confirmation for login to enable account activation.
builder.Services.AddIdentity<ClashUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


//Security
builder.Services.AddScoped<SignInManager<ClashUser>, ClashZone.Security.BannedAwareSignInManager>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();


//Repositories
builder.Services.AddScoped<ITournamentsRepository, TournamentsRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IProductsRepository, ProductsRepository>();
builder.Services.AddScoped<IProductRedeemRepository, ProductRedeemRepository>();
builder.Services.AddScoped<ICoinWalletRepository, CoinWalletRepository>();
builder.Services.AddScoped<IMatchesRepository, MatchesRepository>();
builder.Services.AddScoped<IPlayerStatsRepository, PlayerStatsRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILeagueTeamsRepository, LeagueTeamsRepository>();
builder.Services.AddScoped<ILeagueRepository, LeagueRepository>();

// Business
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IBracketService, BracketService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<ICoinShopService, CoinShopService>();
builder.Services.AddScoped<ICoinWalletService, CoinWalletService>();
builder.Services.AddScoped<IMatchesService, MatchesService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ILeagueTeamService, LeagueTeamService>();
builder.Services.AddScoped<ILeagueService, LeagueService>();

// Riot Games API integration (real League of Legends match data).
// Klucz konfigurujemy poza repo (user-secrets / zmienne środowiskowe);
// gdy brak klucza, integracja jest wyłączona i LoL działa jak "brak danych".
var riotOptions = builder.Configuration.GetSection("Riot").Get<RiotOptions>() ?? new RiotOptions();
builder.Services.AddSingleton(riotOptions);
builder.Services.AddHttpClient<IRiotApiClient, RiotApiClient>(client =>
{
    client.BaseAddress = new Uri(riotOptions.RegionalHost);
    if (riotOptions.IsConfigured)
    {
        client.DefaultRequestHeaders.Add("X-Riot-Token", riotOptions.ApiKey);
    }
});
builder.Services.AddScoped<IRiotMatchService, RiotMatchService>();

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