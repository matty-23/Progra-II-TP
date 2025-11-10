using Medilink.Interfaces;
using Medilink.Models;
using Medilink.Context;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
namespace Medilink.Services
{
    public class InsumoService : IInsumoService
    {
        private readonly MedilinkDbContext _context;
        private readonly HttpClient _httpClient;
        public InsumoService(MedilinkDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }
        public async Task<Insumo> AddInsumo(Insumo insumo)
{
    // Buscar insumo por código (único o identificador alternativo)
    var insumoInventario = await _context.Insumos
        .FirstOrDefaultAsync(i => i.Codigo == insumo.Codigo);

    if (insumoInventario != null)
    {
        // Si existe, sumar cantidad
        insumoInventario.cantidadInventario += insumo.cantidadInventario;
        await _context.SaveChangesAsync();
        return insumoInventario;
    }

    // Si no existe, agregar nuevo insumo
    await _context.Insumos.AddAsync(insumo);
    await _context.SaveChangesAsync();
    return insumo;
}


        public async Task<bool> RestarCantidad(int id, int cantidad)
        {
            var insumo = await GetInsumo(id);
            if (insumo == null || cantidad <= 0 || insumo.cantidadInventario < cantidad)
                return false;
            insumo.cantidadInventario -= cantidad;
            await UpdateInsumo(insumo);
            return true;
        }
        public async Task<Insumo> GetInsumo(int id)
        {
            return await _context.Insumos.FindAsync(id);
        }

        public async Task<bool> DeleteInsumo(int id)
        {
            var insumo = await GetInsumo(id);
            if (insumo == null) return false;
            _context.Insumos.Remove(insumo);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<IEnumerable<Insumo>> GetInsumos()
        {
            return await _context.Insumos.ToListAsync();
        }

        public async Task<bool> UpdateInsumo(Insumo insumo)
        {
            var InsumoExiste = await GetInsumo(insumo.Id);
            if (InsumoExiste == null) return false;
            InsumoExiste.Nombre = insumo.Nombre;
            InsumoExiste.Descripcion = insumo.Descripcion;
            InsumoExiste.cantidadInventario = insumo.cantidadInventario;
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Insumos.AnyAsync(m => m.Id == insumo.Id))
                    return false;
                else
                    throw;
            }
        }

        public async Task<Insumo> PedidoInsumos(Insumo ins, string presentacion, string unidadMedida, string prioridad)
        {

            List<EnviarPedido> items = new List<EnviarPedido>();


            var insumo = _context.Insumos.Find(ins.Id);
            if (insumo != null)
            {
                items.Add(new EnviarPedido
                {
                    Codigo = insumo.Codigo,
                    Nombre = insumo.Nombre,
                    Presentacion = presentacion,
                    Cantidad = insumo.cantidadInventario,
                    UnidadMedida = unidadMedida,
                    Prioridad = prioridad
                });

            }


            var request = new
            {//Ponernos de acuerdo con el otro grupo para tener un campo en comun "codigo" para hacer la request
                hospitalId = "Medilink001",
                fechaPedido = DateTime.UtcNow,
                contacto = "Guardia",
                items = items
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            //Falta hacer las pruebas para conectarse
            var response = await _httpClient.PostAsync("https://localhost:5000/api/reposiciones/pedidos", content);

            if (!response.IsSuccessStatusCode)
                return null;
            //Arreglar que efectivamente sea asi
            var respuestaJson = await response.Content.ReadAsStringAsync();
            var respuesta = JsonSerializer.Deserialize<RespuestaPedido>(respuestaJson);

            if (respuesta.Status == "REJECTED")
                return null;

            insumo.cantidadInventario += respuesta.TotalConfirmado;
            await _context.SaveChangesAsync();

            return  insumo;

        }
    }


    public class RespuestaPedido
    {
        public int PedidoId { get; set; }
        public string Status { get; set; }
        public List<ResumenItem> Items { get; set; }
        public int TotalSolicitado { get; set; }
        public int TotalConfirmado { get; set; }
    }
    public class ResumenItem
    {
        public string Codigo { get; set; }
        public int CantidadSolicitada { get; set; }
        public int CantidadConfirmada { get; set; }
    }
    public class EnviarPedido
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Presentacion { get; set; }
        public int Cantidad { get; set; }
        public string UnidadMedida { get; set; }
        public string Prioridad { get; set; }
    }
}
