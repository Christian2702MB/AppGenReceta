using System;
using System.Collections.Generic;

namespace AppGenReceta.BE
{
    public class LIQ_AuditoriaAnulacionBE
    {
        public int IdOperacion { get; set; }
        public string NP { get; set; }
        public string CodInsumo { get; set; }
        public string DescripcionInsumo { get; set; }
        public string NombreColor { get; set; }
        public int? IdVisita { get; set; }
        public decimal CantidadAnulada { get; set; }
        public string FuenteConsumo { get; set; }
        public string UsuarioAnulacion { get; set; }
        public DateTime FechaAnulacion { get; set; }
        public string FechaAnulacionTexto => FechaAnulacion.ToString("dd/MM/yyyy HH:mm:ss");
        public string MotivoAnulacion { get; set; }
        public string TrazaOriginalSistema { get; set; }
    }
}
