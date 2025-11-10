using System.Runtime.Versioning;
using Medilink.Models;
using Medilink.Services;
using Medilink.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Medilink.DTO;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Medilink.Controllers;

[ApiController]
// [Authorize]
[Route("api/[controller]")]

public class InsumoController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InsumoController> _logger;
    private readonly IInsumoService _insumoService;
    private readonly IConfiguration _configuration;
    public InsumoController(IInsumoService insumoService, HttpClient httpClient, IConfiguration configuration)
    {
        _insumoService = insumoService;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Insumo>>> GetAll()
    {
        var insumos = await _insumoService.GetInsumos();
        return Ok(insumos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Insumo>> GetOneById(int id)
    {
        var insumo = await _insumoService.GetInsumo(id);
        if (insumo == null) return NotFound();
        return Ok(insumo);
    }

    [HttpPost]
    public async Task<ActionResult<Insumo>> Create([FromBody] Insumo insumo)
    {

        var nuevoInsumo = await _insumoService.AddInsumo(insumo);
        return CreatedAtAction(nameof(GetOneById), new { id = nuevoInsumo.Id }, nuevoInsumo);
    }
    [HttpPost("pedido")]
        public async Task<ActionResult<Insumo>> HacerPedido([FromBody] PedidoInsumoRequest request)
        {
            if (request == null || request.IdInsumo <= 0)
                return BadRequest("Datos del pedido inválidos.");

            var insumo = await _insumoService.GetInsumo(request.IdInsumo);
            if (insumo == null) return NotFound();

            var resultado = await _insumoService.PedidoInsumos(
                insumo,
                request.Presentacion,
                request.UnidadMedida,
                request.Prioridad);

            if (resultado == null)
                return BadRequest("No se pudo completar el pedido.");

            return Ok(resultado);
        }
    
    
    [HttpPut("{id}")]
    public async Task<ActionResult> CompleteUpdate([FromBody] Insumo insumo, int id)
    {
        if (id != insumo.Id) return BadRequest();
        var resultado = await _insumoService.UpdateInsumo(insumo);
        if (!resultado) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var insumo = await _insumoService.DeleteInsumo(id);
        if (!insumo) return NotFound($"No se encontró el insumo con ID {id}");
        else return NoContent();
    }
    [HttpPatch("{id}/restar")]
    public async Task<IActionResult> RestarCantidad(int id, [FromQuery] int cantidad)
    {
           var resultado = await _insumoService.RestarCantidad(id, cantidad);

        if (!resultado)
            return NotFound($"No se encontró el insumo con ID {id} o la cantidad a restar es inválida.");

        return Ok($"Se restaron {cantidad} unidades del insumo con ID {id}.");
    }
}