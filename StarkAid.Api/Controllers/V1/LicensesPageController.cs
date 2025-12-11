using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StarkAid.Api.Controllers.V1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class LicensesPageController : ControllerBase
{
    [HttpGet("/licenses.html")]
    [HttpGet("/licenses")]
    [AllowAnonymous]
    public IActionResult GetLicensesPage()
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            // Usar arquivo renomeado para evitar conflito com arquivos estáticos
            var filePath = Path.Combine(currentDir, "wwwroot", "_licenses.html");
            
            // Log para debug
            System.Diagnostics.Debug.WriteLine($"[LicensesPageController] Current Directory: {currentDir}");
            System.Diagnostics.Debug.WriteLine($"[LicensesPageController] File Path: {filePath}");
            System.Diagnostics.Debug.WriteLine($"[LicensesPageController] File Exists: {System.IO.File.Exists(filePath)}");
            
            if (!System.IO.File.Exists(filePath))
            {
                // Tentar caminho alternativo
                var altPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "_licenses.html");
                System.Diagnostics.Debug.WriteLine($"[LicensesPageController] Trying Alt Path: {altPath}");
                System.Diagnostics.Debug.WriteLine($"[LicensesPageController] Alt File Exists: {System.IO.File.Exists(altPath)}");
                
                if (System.IO.File.Exists(altPath))
                {
                    filePath = altPath;
                }
                else
                {
                    // Tentar o nome original como fallback
                    var fallbackPath = Path.Combine(currentDir, "wwwroot", "licenses.html");
                    if (System.IO.File.Exists(fallbackPath))
                    {
                        filePath = fallbackPath;
                    }
                    else
                    {
                        return NotFound(new { 
                            message = "Arquivo licenses.html não encontrado",
                            currentDir = currentDir,
                            baseDir = AppContext.BaseDirectory,
                            filePath = filePath,
                            altPath = altPath,
                            fallbackPath = fallbackPath
                        });
                    }
                }
            }

            var content = System.IO.File.ReadAllText(filePath);
            
            // Definir headers para evitar cache e garantir que seja HTML
            Response.Headers.Append("Content-Type", "text/html; charset=utf-8");
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");
            
            return Content(content, "text/html");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LicensesPageController] Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LicensesPageController] StackTrace: {ex.StackTrace}");
            return StatusCode(500, new { message = "Erro ao carregar página de licenças", error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}

