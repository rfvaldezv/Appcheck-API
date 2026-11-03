using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AppCheck.API.DTOs;
using AppCheck.API.Services;
using AppCheck.Data;
using AppCheck.Data.Entities;

namespace AppCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppCheckDbContext _db;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _config;

    public AuthController(AppCheckDbContext db, JwtService jwtService, IConfiguration config)
    {
        _db = db;
        _jwtService = jwtService;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Activo);

        if (usuario == null)
            return Unauthorized(new { mensaje = "Credenciales inválidas." });

        if (usuario.PasswordHash != request.Password)
            return Unauthorized(new { mensaje = "Credenciales inválidas." });

        var token = _jwtService.GenerarToken(usuario);

        return Ok(new
        {
            token,
            usuario = new
            {
                usuario.UsuarioId,
                usuario.Nombre,
                usuario.Email,
                usuario.Rol,
                usuario.CuentaId
            }
        });
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        var cuentaAppCheck = await _db.Cuentas
            .FirstOrDefaultAsync(c => c.NombreComercial == "AppCheck SA de CV");

        if (cuentaAppCheck == null)
        {
            cuentaAppCheck = new Cuenta
            {
                TipoCuenta = "Independiente",
                NombreComercial = "AppCheck SA de CV",
                RFC = "APC260101ABC",
                Estado = "Activa"
            };
            _db.Cuentas.Add(cuentaAppCheck);
            await _db.SaveChangesAsync();
        }

        var admin = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == "admin@appcheck.com.mx");

        if (admin == null)
        {
            admin = new Usuario
            {
                CuentaId = cuentaAppCheck.CuentaId,
                Email = "admin@appcheck.com.mx",
                PasswordHash = "Admin123!",
                Nombre = "SuperAdmin AppCheck",
                Rol = "SuperAdmin",
                Activo = true
            };
            _db.Usuarios.Add(admin);
            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            mensaje = "Seed ejecutado correctamente.",
            cuentaId = cuentaAppCheck.CuentaId,
            usuarioId = admin.UsuarioId,
            email = admin.Email,
            password = "Admin123!"
        });
    }

    [HttpGet("test-token")]
    [AllowAnonymous]
    public IActionResult TestToken([FromQuery] string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            }, out var validatedToken);

            return Ok(new
            {
                mensaje = "Token válido",
                issuer = _config["Jwt:Issuer"],
                audience = _config["Jwt:Audience"],
                keyLength = _config["Jwt:Key"]!.Length,
                claims = principal.Claims.Select(c => new { c.Type, c.Value })
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensaje = "Token inválido",
                error = ex.Message,
                issuer = _config["Jwt:Issuer"],
                audience = _config["Jwt:Audience"],
                keyLength = _config["Jwt:Key"]?.Length
            });
        }
    }
}
