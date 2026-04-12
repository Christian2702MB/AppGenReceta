using System;
using System.Collections.Generic;

namespace AppGenReceta.BE
{
    public class NPBE
    {
        public Int32 IDNP { get; set; }
        public String NP { get; set; }
        public String Cliente { get; set; }
        public String Estilo { get; set; }
        public String Combo { get; set; }
        public String Tecnica { get; set; }
        public String OperarioUDP { get; set; }
        public String FechaUDP { get; set; }
        public String Concepto { get; set; }

        public String Ubicacion { get; set; }
        public String OperarioProd { get; set; }
        public String FechaProd { get; set; }

        public Int32 PrendasReq { get; set; }
        public String ListaItems { get; set; }

    }
}