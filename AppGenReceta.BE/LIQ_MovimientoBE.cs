using System;
using System.Collections.Generic;

namespace AppGenReceta.BE
{
    public class LIQ_MOV_SolicitudBE
    {
        public int IdSolicitud { get; set; }
        public string TipoMovimiento { get; set; }
        public string TipoAprobador { get; set; }
        public string Estado { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string UsuarioLiquidador { get; set; }
        public string UsuarioAprobador { get; set; }
        public string NumMovimientoERP { get; set; }
        public string Almacen { get; set; }
        public string Observaciones { get; set; }
        public string NroGuia { get; set; }
        
        public List<LIQ_MOV_SolicitudDetalleBE> Detalles { get; set; }

        public LIQ_MOV_SolicitudBE()
        {
            Detalles = new List<LIQ_MOV_SolicitudDetalleBE>();
        }
    }

    public class LIQ_MOV_SolicitudDetalleBE
    {
        public int IdDetalle { get; set; }
        public int IdSolicitud { get; set; }
        public string NP { get; set; }
        public string CodItem { get; set; }
        public string NombreItem { get; set; }
        public string UM { get; set; }
        public decimal Cantidad { get; set; }
        public string CodMermaOrigen { get; set; }
        public string Lote { get; set; }
        public string LoteProv { get; set; }
    }
}
