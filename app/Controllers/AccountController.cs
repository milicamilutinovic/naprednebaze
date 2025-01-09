using app.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System.Security.Claims;

public class AccountController : Controller
{
    private readonly IGraphClient _graphClient; // Pretpostavljamo da koristite Neo4j ili drugi DB klijent

    public AccountController(IGraphClient graphClient)
    {
        _graphClient = graphClient;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        // Traženje korisnika u bazi na osnovu korisničkog imena
        var user = (await _graphClient.Cypher
                 .Match("(u:User {username: $Username})")
                 .WithParam("Username", username)
                 .Return(u => u.As<User>())
                 .ResultsAsync).FirstOrDefault();

        if (user == null)
        {
            // Korisnik nije pronađen
            ViewBag.Error = "Invalid username.";
            return View();
        }

        // Jednostavna validacija šifre (ako je u bazi u običnom tekstu)
        if (password != user.PasswordHash)
        {
            // Ako šifra ne odgovara
            ViewBag.Error = "Invalid password.";
            return View();
        } 

        // Kreiranje Claims za autentifikaciju
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username), // Claim za korisničko ime
        new Claim(ClaimTypes.Email, user.Email)    // Claim za email
    };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Prijavljivanje korisnika
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToAction("Index", "Home"); // Nakon uspešne prijave, preusmeravanje na Home stranicu
    }


    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

       [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string password, string email)
        {
            // Logika za registraciju korisnika
            // Ovde možete dodati logiku za čuvanje korisnika u bazi podataka
            return RedirectToAction("Index", "Home"); // Preusmeravanje na početnu stranicu nakon registracije
        }
    }
