using System;
using System.Collections.Generic;
using AppGenReceta.BE;
using AppGenReceta.DA;

namespace AppGenReceta.BL
{
    public class ConsumoAdicionalBL
    {
        private ConsumoAdicional_DA da = new ConsumoAdicional_DA();
        private InsumoNP_DA insumoDA = new InsumoNP_DA();

        public List<E_ConsumoAdicionalCabecera> ListarRequerimientos(string opcion, string fechaDesde, string fechaHasta, string np)
        {
            return da.ListarRequerimientos(opcion, fechaDesde, fechaHasta, np);
        }

        public string ValidarYObtenerNP(string npBuscado)
        {
            return da.ValidarYObtenerNP(npBuscado);
        }

        public string GenerarRequerimientoCompleto(string np, string observaciones)
        {
            // 1. Obtener estilo de la NP
            string estilo = da.ObtenerEstiloPorNP(np);
            if (string.IsNullOrEmpty(estilo)) 
                throw new Exception("No se encontró el estilo asociado a la NP " + np);

            // 2. Obtener receta (insumos necesarios)
            // Reutilizamos la lógica de cálculo existente basada en estilos
            List<E_InsumoCalculado> receta = insumoDA.CalcularInsumosAgrupacion("TEMP_REQ", estilo);
            if (receta == null || receta.Count == 0) 
                throw new Exception("La NP " + np + " no tiene insumos configurados en su receta.");

            // 3. Insertar Cabecera y capturar ID (Paso 1)
            int numReq = da.InsertarCabecera(np, observaciones);
            if (numReq <= 0) 
                throw new Exception("No se pudo generar el número de requerimiento.");

            // 4. Insertar Detalle iterando (Paso 2 - Manejo de errores individual)
            foreach (var item in receta)
            {
                try
                {
                    da.InsertarDetalleSimple(numReq, item.CodigoInsumo, item.GramosUDP);
                }
                catch (Exception)
                {
                    // Regla Crítica: Si falla un insumo, continuar con el siguiente
                    continue;
                }
            }

            return numReq.ToString();
        }

        public int InsertarCabecera(string np, string observaciones)
        {
            return da.InsertarCabecera(np, observaciones);
        }

        public void InsertarDetalleSimple(int numReq, string codItem, decimal consumo, int lote = 0)
        {
            da.InsertarDetalleSimple(numReq, codItem, consumo, lote);
        }

        public List<E_ConsumoAdicionalDetalle> ListarDetalles(int numReq)
        {
            return da.ListarDetalles(numReq);
        }

        public void EliminarCabecera(int numReq)
        {
            da.EliminarCabecera(numReq);
        }

        public void EliminarDetalle(int numReq, int secu)
        {
            da.EliminarDetalle(numReq, secu);
        }

        public List<E_VoucherQyc> ObtenerVoucherQyc(int numReq)
        {
            return da.ObtenerVoucherQyc(numReq);
        }
    }
}
