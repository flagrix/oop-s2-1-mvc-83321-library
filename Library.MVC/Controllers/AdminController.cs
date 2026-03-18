using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        // GET: /Admin/Roles
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        // POST: /Admin/Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                    TempData["Success"] = $"Role '{roleName}' created successfully.";
                }
                else
                {
                    TempData["Error"] = $"Role '{roleName}' already exists.";
                }
            }
            return RedirectToAction(nameof(Roles));
        }

        // POST: /Admin/Roles/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role != null && role.Name != "Admin") // Protect Admin role from deletion
            {
                await _roleManager.DeleteAsync(role);
                TempData["Success"] = $"Role '{role.Name}' deleted.";
            }
            else if (role?.Name == "Admin")
            {
                TempData["Error"] = "The Admin role cannot be deleted.";
            }
            return RedirectToAction(nameof(Roles));
        }
    }
}
