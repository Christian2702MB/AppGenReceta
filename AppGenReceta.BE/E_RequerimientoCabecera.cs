using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGenReceta.BE
{
    public class E_RequerimientoCabecera
    {
        public string CodArea { get; set; }
        public int NumRequerimiento { get; set; }
        public string FecRequerimiento { get; set; }
        public string CodMotivo { get; set; }
        public string MotivoDesc { get; set; }
        public string Observacion { get; set; }
        public string TrabajadorSolicitante { get; set; }
        public string FecCreacion { get; set; }
    }
}
