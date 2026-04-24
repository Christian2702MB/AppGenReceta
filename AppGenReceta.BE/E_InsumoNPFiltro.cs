using System;

namespace AppGenReceta.BE
{
    public class E_InsumoNPFiltro
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string NP { get; set; }
        public string TipoBusqueda { get; set; }
    }
}
