using Medilink.Models;
using Medilink.Services;
using Medilink.Interfaces;
using Medilink.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace Medilink.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecetaController : ControllerBase
{
    private readonly IRecetaService _recetaService;
    private readonly FirmaDigitalService _firmaService;
    
    public RecetaController(IRecetaService recetaService, FirmaDigitalService firmaService)
    {
        _recetaService = recetaService;
        _firmaService = firmaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Receta>>> GetAll()
    {
        var recetas = await _recetaService.GetRecetas();
        return Ok(recetas);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Receta>> GetOneById(int id)
    {
        var receta = await _recetaService.GetReceta(id);
        if (receta == null) return NotFound();
        return Ok(receta);
    }

    [HttpPost]
    public async Task<ActionResult<Receta>> Create([FromBody] Receta receta, [FromQuery] int idConsulta)
    {
        try
        {
            var nuevaReceta = await _recetaService.AddReceta(receta, idConsulta);
            return CreatedAtAction(nameof(GetOneById), new { id = nuevaReceta.Id }, nuevaReceta);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> CompleteUpdate([FromBody] Receta receta, int id)
    {
        if (id != receta.Id) return BadRequest();
        var resultadoReceta = await _recetaService.UpdateReceta(receta);
        if (!resultadoReceta) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var receta = await _recetaService.DeleteReceta(id);
        if (!receta) return NotFound($"No se encontro la receta con ID {id}");
        return NoContent();
    }

    // ✅ ENDPOINT PARA GENERAR FIRMA - Solo envía el ID
    [HttpPost("generar-firma/{idReceta}")]
    public async Task<IActionResult> GenerarFirma(int idReceta)
    {
        try
        {
            // Buscar la receta
            var receta = await _recetaService.GetReceta(idReceta);
            if (receta == null)
                return NotFound(new { message = $"Receta con ID {idReceta} no encontrada" });

            // Serializar receta completa
            var jsonReceta = JsonSerializer.Serialize(receta, _firmaService.GetFirmaOptions());
            
            // Generar firma
            var firma = _firmaService.GenerarFirma(receta);

            // Devolver objeto con receta y firma separados
            return Ok(new
            {
                receta = receta,
                firma = firma,
                jsonReceta = jsonReceta // Para debug, puedes eliminarlo en producción
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    
    [HttpPost("validar-externa")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidarExterna([FromBody] RecetaConFirmaDto dto)
    {
        try
        {
            // Validar que lleguen los datos
            if (dto == null || string.IsNullOrEmpty(dto.Firma) || dto.Receta == null)
                return BadRequest(new { status = "error", message = "Falta la receta o la firma." });

            // Serializar la receta recibida con las MISMAS opciones que se usaron para firmar
            var jsonReceta = JsonSerializer.Serialize(dto.Receta, _firmaService.GetFirmaOptions());

            // Calcular la firma esperada
            var firmaCalculada = _firmaService.GenerarFirma(dto.Receta);

            // Comparar firmas
            if (firmaCalculada != dto.Firma)
            {
                return BadRequest(new 
                { 
                    status = "error", 
                    message = "Firma digital inválida.",
                    firmaRecibida = dto.Firma,
                    firmaCalculada = firmaCalculada // Para debug
                });
            }

            // Validar datos de receta
            if (dto.Receta.Id == 0)
                return BadRequest(new { status = "error", message = "ID de receta inválido." });

            if (dto.Receta.RecetaMedicamentos == null || dto.Receta.RecetaMedicamentos.Count == 0)
                return BadRequest(new { status = "error", message = "La receta no tiene medicamentos." });

            // Verificar que la receta existe en nuestra base de datos
            var recetaExistente = await _recetaService.GetReceta(dto.Receta.Id);
            if (recetaExistente == null)
                return BadRequest(new { status = "error", message = "Receta no encontrada en el sistema." });

            return Ok(new
            {
                status = "ok",
                mensaje = "Receta válida y confirmada",
                codigo = dto.Receta.Id.ToString(),
                medicamentos = dto.Receta.RecetaMedicamentos.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = "error", message = $"Error al validar: {ex.Message}" });
        }
    }
}