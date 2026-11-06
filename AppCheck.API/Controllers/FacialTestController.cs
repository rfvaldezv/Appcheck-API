using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppCheck.API.Services;

namespace AppCheck.API.Controllers;

[ApiController]
[Route("api/facial")]
[AllowAnonymous]
public class FacialTestController : ControllerBase
{
    private readonly FacialRecognitionService _facial;

    public FacialTestController(FacialRecognitionService facial)
    {
        _facial = facial;
    }

    public class EmbeddingRequest { public string Foto { get; set; } = string.Empty; }

    public class CompareRequest
    {
        public string FotoA { get; set; } = string.Empty;
        public string FotoB { get; set; } = string.Empty;
    }

    [HttpPost("embedding")]
    public IActionResult Embedding([FromBody] EmbeddingRequest request)
    {
        if (string.IsNullOrEmpty(request.Foto))
            return BadRequest(new { mensaje = "Se requiere Foto en Base64." });

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var emb = _facial.GenerarEmbedding(request.Foto);
        var ms = sw.ElapsedMilliseconds;

        if (emb.Length == 0)
            return BadRequest(new { mensaje = "No se detectó rostro.", ms });

        return Ok(new
        {
            dimension = emb.Length,
            ms,
            primeros10 = emb.Take(10).ToArray(),
            norma = Math.Sqrt(emb.Sum(x => (double)x * x))
        });
    }

    [HttpPost("compare")]
    public IActionResult Compare([FromBody] CompareRequest request)
    {
        if (string.IsNullOrEmpty(request.FotoA) || string.IsNullOrEmpty(request.FotoB))
            return BadRequest(new { mensaje = "Se requieren FotoA y FotoB." });

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var embA = _facial.GenerarEmbedding(request.FotoA);
        var msA = sw.ElapsedMilliseconds;

        sw.Restart();
        var embB = _facial.GenerarEmbedding(request.FotoB);
        var msB = sw.ElapsedMilliseconds;

        if (embA.Length == 0) return BadRequest(new { mensaje = "No se detectó rostro en FotoA." });
        if (embB.Length == 0) return BadRequest(new { mensaje = "No se detectó rostro en FotoB." });

        var sim = _facial.CompararRostros(embA, embB);

        return Ok(new
        {
            similitudPorcentaje = sim,
            dimensionEmbedding = embA.Length,
            msGenerarA = msA,
            msGenerarB = msB,
            veredicto = sim >= 75 ? "MISMA PERSONA (alta confianza)"
                      : sim >= 60 ? "POSIBLE coincidencia"
                      : "PERSONAS DISTINTAS"
        });
    }
}
