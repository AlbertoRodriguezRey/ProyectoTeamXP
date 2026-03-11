using Microsoft.EntityFrameworkCore;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Repositories;
using ProyectoTeamXP.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

string connectionString = builder.Configuration.GetConnectionString("TeamXPDatabase");
// Scoped: comparten el mismo DbContext por request (antes era Transient → bug sutil)
builder.Services.AddScoped<RepositoryUsuarios>();
builder.Services.AddScoped<RepositoryClientes>();
builder.Services.AddScoped<RepositorySeguimiento>();
builder.Services.AddScoped<RepositoryNutricion>();
builder.Services.AddScoped<RepositoryRutinas>();
builder.Services.AddScoped<RepositoryFeedback>();
builder.Services.AddScoped<RepositoryRecursos>();
builder.Services.AddScoped<ExcelImportExportService>();
builder.Services.AddSingleton<GoogleDriveService>();
builder.Services.AddDbContext<TeamXPDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Reparar hashes falsos del seed SQL al arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TeamXPDbContext>();
    await DatabaseSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Session ANTES de Authorization (la sesión debe estar disponible durante la autorización)
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
