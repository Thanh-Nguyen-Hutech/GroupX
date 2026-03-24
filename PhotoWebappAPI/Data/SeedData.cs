using Microsoft.AspNetCore.Identity;
using PhotoWebappAPI.Models;

namespace PhotoWebappAPI.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // 1. Danh sách các Role cần có trong hệ thống
            string[] roleNames = { "Admin", "Photographer", "Customer" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Tạo Role nếu chưa tồn tại
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Tạo tài khoản SIÊU ADMIN mẫu
            var adminEmail = "admin@fotoz.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var admin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Hệ Thống Admin",
                    EmailConfirmed = true,
                    Address = "Trụ sở FOTOZ"
                };

                // Thiết lập mật khẩu mặc định: Admin@123
                var createPowerUser = await userManager.CreateAsync(admin, "Admin@123");

                if (createPowerUser.Succeeded)
                {
                    // Gán quyền Admin cho tài khoản này
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}
