using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppCheck.Data;

namespace AppCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppCheckDbContext _db;

    public AdminController(AppCheckDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // LISTAR EMPRESAS (para filtros de reportes)
    // =====================================================
    [HttpGet("empresas")]
    public async Task<IActionResult> ListarEmpresas()
    {
        var rol = User.FindFirst("Rol")?.Value;
        if (rol != "SuperAdmin")
            return Unauthorized(new { mensaje = "Solo SuperAdmin puede ver todas las empresas." });

        var empresas = await _db.Cuentas
            .Select(c => new
            {
                c.CuentaId,
                c.NombreComercial,
                c.VendedorId,
                c.CuentaPadreId,
                c.TipoCuenta
            })
            .OrderBy(c => c.NombreComercial)
            .ToListAsync();

        return Ok(empresas);
    }

    // =====================================================
    // LISTAR VENDEDORES (para filtros de reportes)
    // =====================================================
    [HttpGet("vendedores")]
    public async Task<IActionResult> ListarVendedores()
    {
        var rol = User.FindFirst("Rol")?.Value;
        if (rol != "SuperAdmin")
            return Unauthorized(new { mensaje = "Solo SuperAdmin puede ver todos los vendedores." });

        var vendedores = await _db.Usuarios
            .Where(u => u.Rol == "Vendedor")
            .Select(u => new
            {
                u.UsuarioId,
                u.Nombre,
                u.Email
            })
            .OrderBy(u => u.Nombre)
            .ToListAsync();

        return Ok(vendedores);
    }
}
