using System.Reflection.Metadata;

public class PedidoInsumoRequest
    {
        public int IdInsumo { get; set; }
        public string Presentacion { get; set; }
        public string UnidadMedida { get; set; }
        public string Prioridad { get; set; }
    }