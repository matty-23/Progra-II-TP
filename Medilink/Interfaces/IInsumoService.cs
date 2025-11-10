using Medilink.Models;
namespace Medilink.Interfaces
{
    public interface IInsumoService
    {
        Task<IEnumerable<Insumo>> GetInsumos();
        Task<Insumo> GetInsumo(int id);
        Task<Insumo> AddInsumo(Insumo insumo);
        Task<bool> UpdateInsumo(Insumo insumo);
        Task<bool> RestarCantidad(int id, int cantidad);
        Task<bool> DeleteInsumo(int id);

        Task<Insumo> PedidoInsumos(Insumo insumo, string presentacion, string unidadMedida, string prioridad);
    }
}


