using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Models;

var builder = WebApplication.CreateBuilder(args);
//check check
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<TaskAllocationDBContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
