using System;

namespace AppGenReceta.BE
{
    public class E_InsumoCalculado
    {
        public int ID_CALCULO { get; set; }
        
        // Campos del Stored Procedure
        public string CodigoInsumo { get; set; }
        public string Descripcion { get; set; }
        public string NombreColor { get; set; }
        public string NombrePrueba { get; set; }
        public decimal GramosUDP { get; set; }
        
        // Campos internos/auditoria
        public string USUARIO_PROCESA { get; set; }
        public string SESSION_ID { get; set; }
    }
}
