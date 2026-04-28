using System;

namespace AppGenReceta.BE
{
    public class E_InsumoCalculado
    {
        public int ID_CALCULO { get; set; }
        
        // Campos del Stored Procedure Original
        public string CodigoInsumo { get; set; }
        public string Descripcion { get; set; }
        public string NombreColor { get; set; }
        public string NombrePrueba { get; set; }
        public decimal GramosUDP { get; set; }

        // Nuevos campos dinámicos
        public decimal StockActual { get; set; }
        public decimal CapacidadNumerica { get; set; } // Oculto: Base Matemática de Divisor
        public decimal CantidadAPedir { get; set; } // Salida Calculada Matemática
        public string TipoDespacho { get; set; }
        public int IdProveedorAsignado { get; set; }
        
        // Campos internos/auditoria
        public string USUARIO_PROCESA { get; set; }
        public string SESSION_ID { get; set; }
    }
}
