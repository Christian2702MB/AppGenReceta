using AppGenReceta.BE;
using AppGenReceta.DA;
using System;
using System.Collections.Generic;

namespace AppGenReceta.BL
{
    /// <summary>
    /// Capa de Lógica de Negocio para el módulo de Liquidación de Insumos.
    /// Proxy entre LiquidacionController y Liquidacion_DA.
    /// </summary>
    public class Liquidacion_BL
    {
        public List<LIQ_FormulaBE> ListarFormulas(string fechaInicio, string fechaFin)
        {
            try
            {
                return new Liquidacion_DA().ListarFormulas(fechaInicio, fechaFin);
            }
            catch (Exception ex) { throw ex; }
        }

        public LIQ_FormulaBE ObtenerFormulaCompleta(int idFormula)
        {
            try
            {
                return new Liquidacion_DA().ObtenerFormulaCompleta(idFormula);
            }
            catch (Exception ex) { throw ex; }
        }

        public int CrearFormulaDesdeReceta(int idRecetaOrigen, string usuario)
        {
            try
            {
                return new Liquidacion_DA().CrearFormulaDesdeReceta(idRecetaOrigen, usuario);
            }
            catch (Exception ex) { throw ex; }
        }

        public List<LIQ_NPSinRecepcionBE> ObtenerNPsSinRecepcion()
        {
            try
            {
                return new Liquidacion_DA().ObtenerNPsSinRecepcion();
            }
            catch (Exception ex) { throw ex; }
        }

        public string CrearRecepcionBlanco(string np, string usuario)
        {
            try
            {
                return new Liquidacion_DA().CrearRecepcionBlanco(np, usuario);
            }
            catch (Exception ex) { throw ex; }
        }

        public bool ActualizarFormula(LIQ_FormulaBE entidad)
        {
            try
            {
                return new Liquidacion_DA().ActualizarFormula(entidad);
            }
            catch (Exception ex) { throw ex; }
        }

        public bool EliminarFormula(int idFormula, string usuario, string comentario)
        {
            try
            {
                return new Liquidacion_DA().EliminarFormula(idFormula, usuario, comentario);
            }
            catch (Exception ex) { throw ex; }
        }

        public List<LIQ_RecetaDisponibleBE> ListarRecetasParaFormula()
        {
            try
            {
                return new Liquidacion_DA().ListarRecetasParaFormula();
            }
            catch (Exception ex) { throw ex; }
        }

        public bool CambiarEstadoFormula(int idFormula, string usuario, bool cerrar, string comentario)
        {
            try
            {
                return new Liquidacion_DA().CambiarEstadoFormula(idFormula, usuario, cerrar, comentario);
            }
            catch (Exception ex) { throw ex; }
        }

        // =======================================================================
        // METODOS OPERATIVOS (CONSUMOS, MERMAS, DEVOLUCIONES)
        // =======================================================================

        public bool RegistrarOperacion(LIQ_OperacionBE ope)
        {
            try
            {
                return new Liquidacion_DA().RegistrarOperacion(ope);
            }
            catch (Exception ex) { throw ex; }
        }

        public List<LIQ_LiquidacionConsolidadaBE> ObtenerLiquidacionesConsolidadas(string estado)
        {
            try
            {
                return new Liquidacion_DA().ObtenerLiquidacionesConsolidadas(estado);
            }
            catch (Exception ex) { throw ex; }
        }

        public List<LIQ_MermaStockBE> ObtenerMermasStock()
        {
            try
            {
                return new Liquidacion_DA().ObtenerMermasStock();
            }
            catch (Exception ex) { throw ex; }
        }

        public LIQ_SaldosPopupBE ObtenerSaldosPopup(string np, string codInsumo, string nombreColor = null, int? idVisita = null)
        {
            try
            {
                return new Liquidacion_DA().ObtenerSaldosPopup(np, codInsumo, nombreColor, idVisita);
            }
            catch (Exception ex) { throw ex; }
        }

        public List<LIQ_NPPendienteBE> ObtenerNPsPendientes()
        {
            try
            {
                return new Liquidacion_DA().ObtenerNPsPendientes();
            }
            catch (Exception ex) { throw ex; }
        }

        public bool RegistrarMermaColor(LIQ_MermaColorRegistroBE merma, string usuario)
        {
            try
            {
                return new Liquidacion_DA().RegistrarMermaColor(merma, usuario);
            }
            catch (Exception ex) { throw ex; }
        }

        public List<LIQ_HistorialUsoMermaBE> ObtenerHistorialUsoMerma(string codigoMerma)
        {
            try
            {
                return new Liquidacion_DA().ObtenerHistorialUsoMerma(codigoMerma);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<LIQ_MermaHistoricoBE> ObtenerMermasPorColor(string np, string color)
        {
            try
            {
                return new Liquidacion_DA().ObtenerMermasPorColor(np, color);
            }
            catch (Exception ex) { throw ex; }
        }

        public List<LIQ_AjusteHistoricoBE> ObtenerAjustesPorInsumo(string np, string codInsumo)
        {
            try
            {
                return new Liquidacion_DA().ObtenerAjustesPorInsumo(np, codInsumo);
            }
            catch (Exception ex) { throw ex; }
        }

        public List<LIQ_AjusteHistoricoBE> ObtenerDevolucionesPorInsumo(string np, string codInsumo)
        {
            try
            {
                return new Liquidacion_DA().ObtenerDevolucionesPorInsumo(np, codInsumo);
            }
            catch (Exception ex) { throw ex; }
        }

        // =======================================================================
        // MAQUINA DE ESTADOS
        // =======================================================================

        public LIQ_TransicionResultadoBE AvanzarEstadoNP(string np, string nuevoEstado, string usuario)
        {
            try
            {
                return new Liquidacion_DA().AvanzarEstadoNP(np, nuevoEstado, usuario);
            }
            catch (Exception ex) { throw ex; }
        }

        public void RegistrarNoMermaGlobal(string np, string usuario)
        {
            try
            {
                new Liquidacion_DA().RegistrarNoMermaGlobal(np, usuario);
            }
            catch (Exception ex) { throw ex; }
        }

        public void RegistrarNoAjusteGlobal(string np, string usuario)
        {
            try
            {
                new Liquidacion_DA().RegistrarNoAjusteGlobal(np, usuario);
            }
            catch (Exception ex) { throw ex; }
        }

        public LIQ_TransicionResultadoBE TerminarNP(string np, string destinoGlobal, string usuario)
        {
            try
            {
                return new Liquidacion_DA().TerminarNP(np, destinoGlobal, usuario);
            }
            catch (Exception ex) { throw ex; }
        }

        public string PredecirSiguienteVersionNP(string npOriginal)
        {
            try
            {
                return new Liquidacion_DA().PredecirSiguienteVersionNP(npOriginal);
            }
            catch (Exception ex) { throw ex; }
        }

        public LIQ_TransicionResultadoBE GenerarSiguienteVersionNP(string npOriginal, string usuario, string observacion)
        {
            try
            {
                return new Liquidacion_DA().GenerarSiguienteVersionNP(npOriginal, usuario, observacion);
            }
            catch (Exception ex) { throw ex; }
        }

        public List<string> ObtenerVersionesNP(string baseNP)
        {
            try
            {
                return new Liquidacion_DA().ObtenerVersionesNP(baseNP);
            }
            catch (Exception ex) { throw ex; }
        }

        public List<LIQ_AuditoriaDevolucionBE> ObtenerAuditoriaDevolucionesCentral(string np)
        {
            try
            {
                return new Liquidacion_DA().ObtenerAuditoriaDevolucionesCentral(np);
            }
            catch (Exception ex) { throw ex; }
        }
    }
}
