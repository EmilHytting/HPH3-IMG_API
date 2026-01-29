using Microsoft.AspNetCore.Mvc;
using FluentFTP;
using System.Net;

namespace HPH3_IMG_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IConfiguration configuration, ILogger<FilesController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // POST: api/files/upload
    [HttpPost("upload")]
    public async Task<ActionResult<FileUploadResponse>> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Ingen fil blev uploadet");
        }

        // Validering af fil type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLower();

        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest("Filtype er ikke tilladt. Kun billeder (jpg, jpeg, png, gif, webp) er tilladt.");
        }

        // Validering af fil størrelse (max 5MB)
        const long maxFileSize = 5 * 1024 * 1024; // 5MB
        if (file.Length > maxFileSize)
        {
            return BadRequest("Filen er for stor. Max størrelse er 5MB.");
        }

        try
        {
            // Hent FTP settings fra appsettings
            var ftpHost = _configuration["FtpSettings:Host"];
            var ftpPort = int.Parse(_configuration["FtpSettings:Port"] ?? "21");
            var ftpUsername = _configuration["FtpSettings:Username"];
            var ftpPassword = _configuration["FtpSettings:Password"];
            var ftpUploadPath = _configuration["FtpSettings:UploadPath"];

            // Generer unikt filnavn
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var remotePath = $"{ftpUploadPath}/{uniqueFileName}";

            _logger.LogInformation($"Starter FTP upload: {uniqueFileName} til {ftpHost}");

            // Upload til FTP
            var ftpClient = new AsyncFtpClient(ftpHost, ftpPort);
            ftpClient.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
            
            // Øg timeout til 60 sekunder
            ftpClient.Config.ConnectTimeout = 60000;
            ftpClient.Config.ReadTimeout = 60000;
            ftpClient.Config.DataConnectionConnectTimeout = 60000;
            ftpClient.Config.DataConnectionReadTimeout = 60000;

            using (ftpClient)
            {
                _logger.LogInformation("Forbinder til FTP server...");
                await ftpClient.AutoConnect();
                _logger.LogInformation("FTP forbundet, uploader fil...");

                using (var stream = file.OpenReadStream())
                {
                    var uploaded = await ftpClient.UploadStream(stream, remotePath);
                    if (uploaded != FtpStatus.Success)
                    {
                        _logger.LogError($"FTP upload fejlede: {uploaded}");
                        return StatusCode(500, $"Kunne ikke uploade fil til FTP server: {uploaded}");
                    }
                }
                
                _logger.LogInformation("FTP upload completed");
            }

            // Generer URL (web-accessible URL)
            var fileUrl = $"https://{ftpHost}/SKOLE/emil153s/_ww1/uploads/{uniqueFileName}";

            _logger.LogInformation($"Fil uploadet til FTP: {uniqueFileName}");

            return Ok(new FileUploadResponse
            {
                FileName = uniqueFileName,
                FileUrl = fileUrl,
                FileSize = file.Length,
                Message = "Fil uploadet successfully til FTP server"
            });
        }
        catch (TimeoutException ex)
        {
            _logger.LogError($"FTP timeout: {ex.Message}");
            return StatusCode(504, "FTP upload timeout - serveren reagerer ikke hurtigt nok");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Fejl ved FTP upload: {ex.Message} - {ex.InnerException?.Message}");
            return StatusCode(500, $"Der skete en fejl ved upload af filen: {ex.Message}");
        }
    }
}

public class FileUploadResponse
{
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Message { get; set; } = string.Empty;
}
