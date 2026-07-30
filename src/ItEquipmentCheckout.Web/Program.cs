using System.Globalization;
using System.Text.Json.Serialization;
using ItEquipmentCheckout.Web.Data;
using ItEquipmentCheckout.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("CheckoutDatabase")
    ?? throw new InvalidOperationException("Connection string 'CheckoutDatabase' is required.");
EnsureDatabaseDirectory(connectionString, builder.Environment.ContentRootPath);

builder.Services.AddDbContext<CheckoutDbContext>(options =>
    options.UseSqlite(
        connectionString,
        sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
builder.Services.AddScoped<CheckoutApplicationService>();
builder.Services.AddScoped<CsvEquipmentService>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("sqlite");

WebApplication app = builder.Build();

app.UseExceptionHandler();
var supportedCultures = new[] { new CultureInfo("en-US") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(supportedCultures[0]),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
});
app.UseStaticFiles();
app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapRazorPages();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Status.ToString().ToLowerInvariant()),
        });
    },
});

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CheckoutDbContext>();
    await dbContext.Database.MigrateAsync();
    if (builder.Configuration.GetValue("Database:Seed", defaultValue: true))
    {
        await DatabaseSeeder.SeedAsync(dbContext);
    }
}

await app.RunAsync();

static void EnsureDatabaseDirectory(string connectionString, string contentRoot)
{
    var builder = new SqliteConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(builder.DataSource) ||
        builder.DataSource == ":memory:" ||
        builder.DataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    string path = Path.IsPathRooted(builder.DataSource)
        ? builder.DataSource
        : Path.Combine(contentRoot, builder.DataSource);
    string? directory = Path.GetDirectoryName(path);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

public partial class Program;
