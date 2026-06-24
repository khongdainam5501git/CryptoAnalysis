using System.Net.Http.Headers;
using CryptoAnalysis.Core.Analysis;
using CryptoAnalysis.Core.Charts;
using CryptoAnalysis.Core.Data;
using CryptoAnalysis.Core.Ingestion;
using CryptoAnalysis.Core.Reports;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CryptoDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.Configure<CoinGeckoOptions>(
    builder.Configuration.GetSection(CoinGeckoOptions.Section));

builder.Services.AddHttpClient<CoinGeckoClient>((sp, client) =>
{
    var cfg = builder.Configuration.GetSection(CoinGeckoOptions.Section);
    var baseUrl = cfg["BaseUrl"] ?? "https://api.coingecko.com/api/v3/";
    var apiKey = cfg["ApiKey"] ?? string.Empty;

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    if (!string.IsNullOrWhiteSpace(apiKey))
        client.DefaultRequestHeaders.Add("x-cg-demo-api-key", apiKey);
});

builder.Services.AddScoped<IngestionService>();
builder.Services.AddScoped<AnalysisService>();
builder.Services.AddSingleton<PriceChartService>();
builder.Services.AddScoped<PdfReportService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CryptoDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
