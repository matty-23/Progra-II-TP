using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Medilink.Models
{
    public class Receta
    {
        public int Id { get; set; }
        public int Estado { get; set; }
        public DateTime FechaVencimiento { get; set; }
        
        // ✅ NO ignorar en JSON para que se incluya en la firma
        public ICollection<RecetaMedicamento> RecetaMedicamentos { get; set; } = new List<RecetaMedicamento>();
    }

    public class RecetaMedicamento
    {
        public int Id { get; set; }
        
        [JsonIgnore] // ✅ Ignorar en JSON
        public int IdReceta { get; set; }
        
        public int IdMedicamento { get; set; }
        
        [JsonIgnore] // ✅ Ignorar navegación
        [ValidateNever]
        public Medicamento? Medicamento { get; set; }
        
        public int Cantidad { get; set; }
    }
}