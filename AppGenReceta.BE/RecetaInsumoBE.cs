using System;

namespace AppGenReceta.BE
{
    public class RecetaInsumoBE
    {
        public string CodigoInsumo { get; set; }

        public decimal Cantidad { get; set; }
        public string Descripcion { get; set; }
        public double GramosUDP { get; set; }
        public double ConsumoUDP { get; set; }
        public double GramosProd { get; set; }
        public double ConsumoProd { get; set; }
        public string Stock { get; set; }
        public string Unid_Med { get; set; }
    }
}
