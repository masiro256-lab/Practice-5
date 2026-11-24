using Microsoft.AspNetCore.Identity;

namespace ProductCatalogApp.Api.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "User", "Admin" })
        {
            if (!await roles.RoleExistsAsync(role))
                Check(await roles.CreateAsync(new IdentityRole(role)));
        }

        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(password)) return;
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.FindByNameAsync("admin");
        if (user == null)
        {
            user = new IdentityUser { UserName = "admin" };
            Check(await users.CreateAsync(user, password));
        }
        if (!await users.IsInRoleAsync(user, "Admin"))
            Check(await users.AddToRoleAsync(user, "Admin"));
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
