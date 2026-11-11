using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppCheck.Data;
using AppCheck.Data.Entities;
using AppCheck.API.DTOs;

namespace AppCheck.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SucursalController : ControllerBase
    {
        private readonly AppCheckDbContext _db;
        public SucursalController(AppCheckDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
            if (string.IsNullOrEmpty(cuentaIdClaim)) return Unauthorized();
            
            var cuentaId = Guid.Parse(cuentaIdClaim);
            var sucursales = await _db.Sucursales
                .Where(s => s.CuentaId == cuentaId && s.Activa)
                .Select(s => new { s.SucursalId, s.Nombre, s.Direccion, s.Latitud, s.Longitud, s.RadioMetros })
                .ToListAsync();
                
            return Ok(sucursales);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearSucursalRequest req)
        {
            var cuentaId = Guid.Parse(User.FindFirst("CuentaId")?.Value!);
            
            var nueva = new Sucursal
            {
                SucursalId = Guid.NewGuid(),
                CuentaId = cuentaId,
                Nombre = req.Nombre,
                Direccion = req.Direccion,
                Latitud = req.Latitud,
                Longitud = req.Longitud,
                RadioMetros = req.RadioMetros
            };

            _db.Sucursales.Add(nueva);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Sucursal creada", sucursalId = nueva.SucursalId });
        }
    }
}
