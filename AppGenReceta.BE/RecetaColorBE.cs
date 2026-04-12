using System;
using System.Collections.Generic;

namespace AppGenReceta.BE
{
    public class RecetaColorBE
    {
        public int IdColor { get; set; }
        public string CodigoColor { get; set; }
        public string NombreColor { get; set; }
        public string Combo { get; set; }
        public List<RecetaInsumoBE> Insumos { get; set; }
    }
}
