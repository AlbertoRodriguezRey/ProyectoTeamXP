using Microsoft.EntityFrameworkCore;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Repositories;
using ProyectoTeamXP.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

string connectionString = builder.Configuration.GetConnectionString("TeamXPDatabase");
builder.Services.AddTransient<RepositoryUsuarios>();
builder.Services.AddTransient<RepositoryClientes>();
builder.Services.AddTransient<RepositorySeguimiento>();
builder.Services.AddTransient<RepositoryNutricion>();
builder.Services.AddTransient<RepositoryRutinas>();
builder.Services.AddTransient<RepositoryFeedback>();
builder.Services.AddTransient<RepositoryRecursos>();
builder.Services.AddScoped<ExcelImportExportService>();
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

app.UseAuthorization();

app.MapStaticAssets();
app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
