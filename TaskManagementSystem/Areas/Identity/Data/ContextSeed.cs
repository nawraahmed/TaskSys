
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp;

namespace TaskManagementSystem.Areas.Identity.Data
{
    public class ContextSeed
    {
        public static async Task SeedRolesAsync(UserManager<IdentityUsers> userManager, RoleManager<IdentityRole> roleManager) { 
        if (roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            }
        }

        public static async Task SeedAdminAsync(UserManager<IdentityUsers> userManager, RoleManager<IdentityRole> roleManager) {
            var defaultUser = new IdentityUsers
            {
                UserName = "Admin",
                Email = "Admin@test.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            if (userManager.Users.All(u=>u.Id != defaultUser.Id)) {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "Test@123");
                    await userManager.AddToRoleAsync(defaultUser, "Admin");

                }
            
            }
        
        }
    }
}
