using AppGenReceta.BE;
using AppGenReceta.DA;
using System.Collections.Generic;
using System.Data;

namespace AppGenReceta.BL
{
    public class LiquidacionStockReq_BL
    {
        private LiquidacionStockReq_DA _da;
        private ConsumoAdicional_DA _daConsumoAdicional;

        public LiquidacionStockReq_BL()
        {
            _da = new LiquidacionStockReq_DA();
            _daConsumoAdicional = new ConsumoAdicional_DA();
        }

        public List<LIQ_StockInsumoBE> ListarStockActual()
        {
            return _da.ListarStockActual();
        }

        public DataTable ObtenerMatrizCruzadaConsumos()
        {
            return _da.ObtenerMatrizCruzadaConsumos();
        }

        public List<LIQ_RequerimientoBE> ListarRequerimientosPendientes()
        {
            return _da.ListarRequerimientosPendientes();
        }

        public List<LIQ_REQ_RecepcionBE> ListarRecepcionesHistoricas(string fechaDesde, string fechaHasta)
        {
            return _da.ListarRecepcionesHistoricas(fechaDesde, fechaHasta);
        }

        public List<LIQ_RequerimientoBE> ListarRequerimientosAJAX(string opcion, string fechaDesde, string fechaHasta, string np = "", int? numReqBusqueda = null)
        {
            return _da.ListarRequerimientosAJAX(opcion, fechaDesde, fechaHasta, np, numReqBusqueda);
        }

        public List<E_ConsumoAdicionalDetalle> ObtenerDetalleRequerimiento(int numReq)
        {
            return _daConsumoAdicional.ListarDetalles(numReq);
        }

        public List<LIQ_REQ_RecepcionDetalleBE> ObtenerDetalleRecepcion(int numReq)
        {
            return _da.ObtenerDetalleRecepcion(numReq);
        }

        public string ConfirmarRecepcion(int numRequerimiento, string codOrdPro, string motivo, string usuarioRecepcion)
        {
            // 1. Validar que exista la fórmula para esta NP
            if (!_da.ExisteFormulaParaNP(codOrdPro))
            {
                throw new System.Exception("No se puede recepcionar porque no tiene fórmula creada. La fórmula debe aparecer en la pestaña 'Mantenimiento de Fórmulas'.");
            }

            // 2. Obtener el detalle real de "ConsumoAdicional" 
            // ya que el front no nos manda los insumos, solo confirma la cabecera.
            // Para eso, consultamos el DA de ConsumoAdicional opcion 4 (Detalle)
            
            var detalles = _daConsumoAdicional.ListarDetalles(numRequerimiento);
            if (detalles == null || detalles.Count == 0)
            {
                throw new System.Exception("No se encontró detalle para el requerimiento " + numRequerimiento);
            }

            // Armar el XML para enviar al SP de ConfirmarRecepcion
            string xmlDetalle = "<Detalles>";
            foreach (var det in detalles)
            {
                xmlDetalle += $"<Detalle><CodInsumo>{det.CodItem}</CodInsumo><Descripcion>{det.Nombre}</Descripcion><Cantidad>{det.ConsumoRequerido}</Cantidad><UM>{det.Unidad}</UM><Lote>{det.Lote}</Lote></Detalle>";
            }
            xmlDetalle += "</Detalles>";

            return _da.ConfirmarRecepcion(numRequerimiento, codOrdPro, motivo, usuarioRecepcion, xmlDetalle);
        }

        public string RegistrarCargaInicial(string codInsumo, string descripcion, string unidadMedida, decimal pesoGramos, string usuario)
        {
            return _da.RegistrarCargaInicial(codInsumo, descripcion, unidadMedida, pesoGramos, usuario);
        }
    }
}
