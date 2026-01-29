using HPH3_IMG_API.Data;
using HPH3_IMG_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentFTP;
using System.Net;

namespace HPH3_IMG_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApplicationDbContext context, IConfiguration configuration, ILogger<UsersController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }

    // GET: api/users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return user;
    }

    // POST: api/users (med profil billede upload)
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromForm] CreateUserWithImageDto createUserDto)
    {
        string? profileImageUrl = null;

        // Hvis der er en billedfil, upload den først
        if (createUserDto.ProfileImageFile != null)
        {
            profileImageUrl = await UploadProfileImage(createUserDto.ProfileImageFile);
            if (profileImageUrl == null)
            {
                return BadRequest("Kunne ikke uploade profilbillede");
            }
        }

        var user = new User
        {
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            Email = createUserDto.Email,
            Password = createUserDto.Password,
            ProfileImage = profileImageUrl ?? createUserDto.ProfileImage
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    // PUT: api/users/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromForm] UpdateUserWithImageDto updateUserDto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        // Hvis der er en ny billedfil, upload den
        if (updateUserDto.ProfileImageFile != null)
        {
            var profileImageUrl = await UploadProfileImage(updateUserDto.ProfileImageFile);
            if (profileImageUrl != null)
            {
                user.ProfileImage = profileImageUrl;
            }
        }

        user.FirstName = updateUserDto.FirstName ?? user.FirstName;
        user.LastName = updateUserDto.LastName ?? user.LastName;
        user.Email = updateUserDto.Email ?? user.Email;
        if (!string.IsNullOrEmpty(updateUserDto.Password))
        {
            user.Password = updateUserDto.Password;
        }
        if (!string.IsNullOrEmpty(updateUserDto.ProfileImage))
        {
            user.ProfileImage = updateUserDto.ProfileImage;
        }
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/users/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Helper method for uploading profile images
    private async Task<string?> UploadProfileImage(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            // Validering af fil type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                _logger.LogWarning($"Filtype ikke tilladt: {fileExtension}");
                return null;
            }

            // Validering af fil størrelse (max 5MB)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                _logger.LogWarning($"Fil for stor: {file.Length} bytes");
                return null;
            }

            // Hent FTP settings
            var ftpHost = _configuration["FtpSettings:Host"];
            var ftpPort = int.Parse(_configuration["FtpSettings:Port"] ?? "21");
            var ftpUsername = _configuration["FtpSettings:Username"];
            var ftpPassword = _configuration["FtpSettings:Password"];
            var ftpUploadPath = _configuration["FtpSettings:UploadPath"];

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var remotePath = $"{ftpUploadPath}/{uniqueFileName}";

            _logger.LogInformation($"Uploader profilbillede: {uniqueFileName}");

            var ftpClient = new AsyncFtpClient(ftpHost, ftpPort);
            ftpClient.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
            ftpClient.Config.ConnectTimeout = 60000;
            ftpClient.Config.ReadTimeout = 60000;
            ftpClient.Config.DataConnectionConnectTimeout = 60000;
            ftpClient.Config.DataConnectionReadTimeout = 60000;

            using (ftpClient)
            {
                await ftpClient.AutoConnect();

                using (var stream = file.OpenReadStream())
                {
                    var uploaded = await ftpClient.UploadStream(stream, remotePath);
                    if (uploaded != FtpStatus.Success)
                    {
                        _logger.LogError($"FTP upload fejlede: {uploaded}");
                        return null;
                    }
                }
            }

            var fileUrl = $"https://{ftpHost}/SKOLE/emil153s/_ww1/uploads/{uniqueFileName}";
            _logger.LogInformation($"Profilbillede uploadet: {uniqueFileName}");

            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Fejl ved profilbillede upload: {ex.Message}");
            return null;
        }
    }
}

public class CreateUserWithImageDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public IFormFile? ProfileImageFile { get; set; }
    public string? ProfileImage { get; set; }
}

public class UpdateUserWithImageDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public IFormFile? ProfileImageFile { get; set; }
    public string? ProfileImage { get; set; }
}

public class CreateUserDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? ProfileImage { get; set; }
}

public class UpdateUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ProfileImage { get; set; }
}
