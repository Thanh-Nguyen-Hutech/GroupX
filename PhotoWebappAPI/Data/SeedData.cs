using Microsoft.AspNetCore.Identity;
using PhotoWebappAPI.Models;

namespace PhotoWebappAPI.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // 1. Danh sách các quyền (Roles) cần tạo
            string[] roleNames = { "Admin", "Customer", "Photographer", "Guest" };

            // Lặp qua từng quyền, nếu trong Database chưa có thì tạo mới
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Lệnh này sẽ tự động sinh mã GUID và điền vào bảng AspNetRoles
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Khởi tạo tài khoản Admin mặc định
            string adminEmail = "admin@system.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            // Nếu chưa có tài khoản admin này thì tạo mới
            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true // Bỏ qua bước xác nhận email
                };

                // Lệnh này sẽ mã hóa password, sinh ID và lưu vào bảng AspNetUsers
                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin@123!");
                if (createPowerUser.Succeeded)
                {
                    // 3. Nối tài khoản vừa tạo với quyền Admin (Lưu vào bảng AspNetUserRoles)
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
