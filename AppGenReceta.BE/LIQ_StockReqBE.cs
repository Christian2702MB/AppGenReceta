using System;

namespace AppGenReceta.BE
{
    public class LIQ_StockInsumoBE
    {
        public string CodInsumo { get; set; }
        public string Descripcion { get; set; }
        public string UnidadMedida { get; set; }
        public decimal StockActual { get; set; }
        public string FechaModificacion { get; set; }
    }

    public class LIQ_RequerimientoBE
    {
        public int NumRequerimiento { get; set; }
        public string CodOrdPro { get; set; }
        public string Motivo { get; set; }
        public string FecCreacion { get; set; }
        public string Partida { get; set; }
        public string Cliente { get; set; }
        public string Observaciones { get; set; }
        // Se asume que viene cruzado o de las consultas previas
    }

    public class LIQ_RequerimientoDetalleBE
    {
        public int NumRequerimiento { get; set; }
        public int Secuencia { get; set; }
        public string CodItem { get; set; }
        public string Nombre { get; set; }
        public decimal ConsumoRequerido { get; set; }
        public int Lote { get; set; }
        public string Unidad { get; set; }
    }

    public class LIQ_REQ_RecepcionBE
    {
        public int NumRequerimiento { get; set; }
        public string CodOrdPro { get; set; }
        public string Motivo { get; set; }
        public string Estado { get; set; }
        public string FechaRecepcion { get; set; }
        public string UsuarioRecepcion { get; set; }
        public string Observaciones { get; set; }
    }

    public class LIQ_REQ_RecepcionDetalleBE
    {
        public int IdRecepcionDetalle { get; set; }
        public int NumRequerimiento { get; set; }
        public string CodInsumo { get; set; }
        public decimal CantidadRecibida { get; set; }
        public string Lote { get; set; }
    }
}
