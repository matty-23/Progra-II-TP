using Medilink.Context;
using Medilink.Interfaces;
using Medilink.Models;
using Medilink.Services;
using Microsoft.EntityFrameworkCore;
namespace Medilink.Services
{
    public class RecetaService : IRecetaService
    {
        private readonly MedilinkDbContext _dbContext;
        private readonly IMedicamentoService _medService;
        public RecetaService(MedilinkDbContext dbContext, IMedicamentoService medService)
        {
            _dbContext = dbContext;
            _medService = medService;
        }
        
        public async Task<IEnumerable<Receta>> GetRecetas()
{
    return await _dbContext.Recetas
        .Include(r => r.RecetaMedicamentos)
            .ThenInclude(rm => rm.Medicamento)
        .ToListAsync();
}

        public async Task<Receta> GetReceta(int id)
{
    return await _dbContext.Recetas
        .Include(r => r.RecetaMedicamentos)         // incluye la tabla intermedia
            .ThenInclude(rm => rm.Medicamento)      // incluye los medicamentos ligados a esa tabla
        .FirstOrDefaultAsync(r => r.Id == id);
}

        public async Task<Receta> AddReceta(Receta receta, int idConsulta)
{
    var consulta = await _dbContext.Consultas.FindAsync(idConsulta);
    if (consulta == null)
        throw new InvalidOperationException($"No se encontró una consulta médica con ID {idConsulta}.");

    // Opcional: validar que RecetaMedicamentos no sea null
    if (receta.RecetaMedicamentos != null)
    {
        foreach (var recetaMed in receta.RecetaMedicamentos)
        {
            // Cargar el medicamento existente para asignarlo y evitar inserciones duplicadas
            var medicamentoExistente = await _dbContext.Medicamentos.FindAsync(recetaMed.IdMedicamento);
            if (medicamentoExistente == null)
                throw new InvalidOperationException($"No se encontró medicamento con ID {recetaMed.IdMedicamento}.");

            recetaMed.Medicamento = medicamentoExistente;
            // Asegúrate de que IdReceta está correcto o déjalo que EF lo maneje
        }
    }

    // Agregar receta con sus medicamentos ya cargados
    await _dbContext.Recetas.AddAsync(receta);
    await _dbContext.SaveChangesAsync();

    consulta.IdReceta = receta.Id;
    consulta.Receta = receta;

    await _dbContext.SaveChangesAsync();

    return receta;
}

        public async Task<bool> UpdateReceta(Receta receta)
        {
            var recetaModificada = await GetReceta(receta.Id);
            if (recetaModificada == null) return false;

            //Falta ver bien la parte de los medicamentos dentro de la receta
            recetaModificada.Estado = receta.Estado;
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteReceta(int id)
        {
            var recetaAeliminar = await GetReceta(id);
            if (recetaAeliminar == null) return false;
            _dbContext.Recetas.Remove(recetaAeliminar);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<bool> AgregarMedicamento(int idReceta, int idMed, int cantidad)
        {
            return false;
    var receta = await _dbContext.Recetas
        .Include(r => r.RecetaMedicamentos)
        .FirstOrDefaultAsync(r => r.Id == idReceta);
    if (receta == null || receta.Estado != 0 || cantidad <= 0)
        return false;

        var medExistente = await _dbContext.Medicamentos.FindAsync(idMed);
            
    if (medExistente == null)
        return false;

    var recetaMed = new RecetaMedicamento
    {
        IdReceta = receta.Id,
        IdMedicamento = medExistente.Id,  // Asegúrate de poner el id correcto
        Medicamento = medExistente,       // Asignas el medicamento cargado
        Cantidad = cantidad
    };

    receta.RecetaMedicamentos.Add(recetaMed);

    await _dbContext.SaveChangesAsync();
    return true;
}


        }
    }
