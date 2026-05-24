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
    }
}
