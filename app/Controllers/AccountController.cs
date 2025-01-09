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
    public async Task<IActionResult> Register([FromBody] User user)
    {
        if (user == null)
        {
            return BadRequest("User data is required.");
        }

        // Validacija unosa korisnika
        if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.PasswordHash))
        {
            return BadRequest("Username, Email, and Password are required.");
        }

        try
        {
            // Automatski postavi vrednosti za UserId, CreatedAt, i IsAdmin
            user.UserId = Guid.NewGuid().ToString();
            user.CreatedAt = DateTime.UtcNow; // Trenutno vreme u UTC formatu
            user.IsAdmin = false;            // Admin je podrazumevano false

            // Kreiraj novog korisnika u bazi
            var query = _graphClient.Cypher
                .Create("(u:User {userId: $UserId, username: $Username, fullName: $FullName, email: $Email, passwordHash: $PasswordHash, profilePicture: $ProfilePicture, bio: $Bio, createdAt: $CreatedAt, isAdmin: $IsAdmin})")
                .WithParam("UserId", user.UserId)
                .WithParam("Username", user.Username)
                .WithParam("FullName", user.FullName ?? string.Empty) // Prazan string ako FullName nije prosleđen
                .WithParam("Email", user.Email)
                .WithParam("PasswordHash", user.PasswordHash)
                .WithParam("ProfilePicture", user.ProfilePicture ?? "default.png") // Podrazumevana slika
                .WithParam("Bio", user.Bio ?? "New user")
                .WithParam("CreatedAt", user.CreatedAt)
                .WithParam("IsAdmin", user.IsAdmin);

            await query.ExecuteWithoutResultsAsync();

            // Uspešna registracija
            return CreatedAtAction(nameof(Register), new { id = user.UserId }, user);
        }
        catch (Exception ex)
        {
            // Obrada grešaka
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

}
