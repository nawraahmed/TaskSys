using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using TaskManagementSystem.Data;
using TaskManagementSystem.Areas.Identity.Data;

var builder = WebApplication.CreateBuilder(args);
//check check
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<TaskAllocationDBContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddDbContext<IdentityContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("IdentityContextConnection")
    ));

builder.Services.AddDefaultIdentity<IdentityUsers>()
    .AddRoles<IdentityRole>() // Add this line to register RoleManager<IdentityRole>
    .AddEntityFrameworkStores<IdentityContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();;

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//we're using MVVM and MVC
app.MapRazorPages();

app.Run();
