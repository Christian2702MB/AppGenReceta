using AppGenReceta.BE;
using AppGenReceta.DA;
using System;
using System.Collections.Generic;

namespace AppGenReceta.BL
{
    public class LIQ_MovimientoBL
    {
        private LIQ_MovimientoDA da = new LIQ_MovimientoDA();

        public List<LIQ_MOV_SolicitudBE> ListarSolicitudes(string usuarioActual, string rolUsuario)
        {
            return da.ListarSolicitudes(usuarioActual, rolUsuario);
        }

        public int RegistrarSolicitud(LIQ_MOV_SolicitudBE solicitud)
        {
            return da.RegistrarSolicitud(solicitud);
        }

        public void AprobarSolicitud(int idSolicitud, string usuarioAprobador, out string numMovimientoERP)
        {
            da.AprobarSolicitud(idSolicitud, usuarioAprobador, out numMovimientoERP);
        }

        public void ObservarSolicitud(int idSolicitud, string usuarioAprobador, string observacion)
        {
            da.ObservarSolicitud(idSolicitud, usuarioAprobador, observacion);
        }

        public List<LIQ_MOV_SolicitudDetalleBE> ObtenerDetalles(int idSolicitud)
        {
            return da.ObtenerDetalles(idSolicitud);
        }

        public void EjecutarMovimientoSTK(string observaciones, DateTime fechaMovimiento, string usuario, List<string> nps)
        {
            da.EjecutarMovimientoSTK(observaciones, fechaMovimiento, usuario, nps);
        }
    }
}
