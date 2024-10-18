using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PartTwoProg.Controllers
{
    public class AppRolesController : Controller
    {
        // RoleManager service for handling role-related operations
        private readonly RoleManager<IdentityRole> _roleManager;

        // Constructor to inject the RoleManager dependency
        public AppRolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        // Action to display all roles available in the system
        public IActionResult Index()
        {
            // Retrieve the list of roles from the RoleManager
            var roles = _roleManager.Roles;

            // Pass the roles to the view for rendering
            return View(roles);
        }

        // GET: Create a new role (Display the Create view)
        [HttpGet]
        public IActionResult Create()
        {
            // Render the Create view
            return View();
        }

        // POST: Create a new role (Handle form submission)
        [HttpPost]
        public async Task<IActionResult> Create(IdentityRole model)
        {
            // Check if the role already exists
            if (!await _roleManager.RoleExistsAsync(model.Name))
            {
                // If the role doesn't exist, create a new one
                await _roleManager.CreateAsync(new IdentityRole(model.Name));
            }

            // Redirect to the Index action after creating the role
            return RedirectToAction("Index");
        }
    }
}
