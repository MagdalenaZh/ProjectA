using EProject.Web.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using EProject.Web.Models;
using System.Security.Claims;

namespace EProject.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

           // Login with email and password 
            var user = _context.UserAccount.FirstOrDefault(x => x.Email == model.Email);

            if (user == null)
            {
               // Message if email does not exist 
                ModelState.AddModelError("", "Email does not exist in the system.");
                return View(model);
            }

            if (user.Password != model.Password)
            {
                // Message if password is wrong 
                ModelState.AddModelError("", "Password is wrong.");
                return View(model);
            }

            // Success Logic
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            //Redirect welcome screen on success 
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the home page after logout 
            return RedirectToAction("Index", "Home");
        }
    }
}