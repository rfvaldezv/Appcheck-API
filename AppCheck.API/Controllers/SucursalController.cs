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

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(Guid id, [FromBody] CrearSucursalRequest req)
        {
            var cuentaId = Guid.Parse(User.FindFirst("CuentaId")?.Value!);
            var sucursal = await _db.Sucursales.FirstOrDefaultAsync(s => s.SucursalId == id && s.CuentaId == cuentaId);

            if (sucursal == null) return NotFound(new { mensaje = "Sucursal no encontrada." });

            sucursal.Nombre = req.Nombre;
            sucursal.Direccion = req.Direccion;
            sucursal.Latitud = req.Latitud;
            sucursal.Longitud = req.Longitud;
            sucursal.RadioMetros = req.RadioMetros;

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Sucursal actualizada correctamente." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            var cuentaId = Guid.Parse(User.FindFirst("CuentaId")?.Value!);
            var sucursal = await _db.Sucursales.FirstOrDefaultAsync(s => s.SucursalId == id && s.CuentaId == cuentaId);

            if (sucursal == null) return NotFound(new { mensaje = "Sucursal no encontrada." });

            // 🛡️ Validar que no tenga empleados asignados
            var tieneEmpleados = await _db.Empleados.AnyAsync(e => e.SucursalId == id && e.Estado == "Activo");
            if (tieneEmpleados)
                return BadRequest(new { mensaje = "No se puede eliminar la sucursal porque tiene empleados activos asignados." });

            // Borrado lógico (o físico, según prefieras. Aquí usamos lógico)
            sucursal.Activa = false;
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Sucursal eliminada correctamente." });
        }
    }
}
