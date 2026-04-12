using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGenReceta.BE
{
    public class Respuesta_Json
    {        
        public int Codigo { get; set; }
        public string Mensaje_Respuesta { get; set; }
        public List<Dictionary<string, object>> Archivos { get; set; }
    }
}
