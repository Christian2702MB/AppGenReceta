using System;
using System.Collections.Generic;
using System.Linq;
using AppGenReceta.BE;
using AppGenReceta.DA;

namespace AppGenReceta.BL
{
    public class InsumoNP_BL
    {
        private InsumoNP_DA da = new InsumoNP_DA();

        public List<E_InsumoNP> BuscarNPs(E_InsumoNPFiltro filtro)
        {
            if (filtro == null) throw new ArgumentNullException(nameof(filtro));
            
            // Validar que el rango máximo sea 31 días
            TimeSpan diff = filtro.FechaFin - filtro.FechaInicio;
            if (diff.TotalDays > 31)
            {
                throw new Exception("El rango de fechas de búsqueda no puede exceder los 31 días.");
            }

            if (filtro.TipoBusqueda == "NP" && !string.IsNullOrWhiteSpace(filtro.NP))
            {
                return da.ObtenerNPPorCodigo(filtro.NP.Trim(), filtro.FechaInicio, filtro.FechaFin);
            }
            else
            {
                return da.ObtenerNPsPorRango(filtro.FechaInicio, filtro.FechaFin);
            }
        }

        public List<E_NpAutocomplete> AutocompleteNP(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 4)
            {
                return new List<E_NpAutocomplete>();
            }
            
            return da.BuscarNPsAutocomplete(texto.Trim()).Take(20).ToList();
        }

        public E_InsumoNPFiltro ConstruirFiltroDefecto()
        {
            DateTime now = DateTime.Today;
            DateTime firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);
            DateTime lastDay = firstDayOfThisMonth.AddMonths(1).AddDays(-1);

            return new E_InsumoNPFiltro
            {
                FechaInicio = now,
                FechaFin = lastDay,
                TipoBusqueda = "RANGO",
                NP = ""
            };
        }

        public List<E_InsumoNP> AgregarNPExterna(List<E_InsumoNP> listaActual, string codigoNP)
        {
            if (string.IsNullOrWhiteSpace(codigoNP)) return listaActual;
            
            codigoNP = codigoNP.Trim();
            if (listaActual == null) listaActual = new List<E_InsumoNP>();

            // Validar que no exista en la lista actual
            if (listaActual.Any(x => x.NP.Equals(codigoNP, StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception($"La NP {codigoNP} ya se encuentra en la lista.");
            }

            // Buscar en los últimos 18 meses
            DateTime fechaFin = DateTime.Now;
            DateTime fechaInicio = fechaFin.AddMonths(-18);

            List<E_InsumoNP> npEncontradas = da.ObtenerNPPorCodigo(codigoNP, fechaInicio, fechaFin);
            
            if (npEncontradas.Count == 0)
            {
                throw new Exception($"La NP {codigoNP} no fue encontrada en los registros de los últimos 18 meses.");
            }

            // Agregar a la lista actual y devolver
            listaActual.AddRange(npEncontradas);
            return listaActual;
        }

        // ==========================================
        // PASO 2: CÁLCULOS
        // ==========================================
        public string GuardarAgrupacionNP(List<E_InsumoNP> lista, string usuarioParams)
        {
            if (lista == null || lista.Count == 0) throw new Exception("No hay NPs para agrupar.");
            
            // Generamos un GUID de sesión para rastrear esta agrupación temporalmente/permanentemente
            string sessionId = Guid.NewGuid().ToString();
            bool guardado = da.GuardarAgrupacionNP(lista, sessionId, usuarioParams);

            if (!guardado) throw new Exception("Error interno al intentar guardar la agrupación en Base de Datos.");

            return sessionId;
        }

        public List<E_InsumoNP> ObtenerAgrupacionNP(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return new List<E_InsumoNP>();
            return da.ObtenerAgrupacionNP(sessionId);
        }

        public List<E_InsumoCalculado> CalcularInsumos(string sessionId, string estilosCsv)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) throw new Exception("Session ID inválido.");
            return da.CalcularInsumosAgrupacion(sessionId, estilosCsv);
        }

        public bool GuardarInsumosCalculados(List<E_InsumoCalculado> calculados, string sessionId, string usuario)
        {
            if (calculados == null || !calculados.Any()) throw new Exception("La lista de insumos calculados está vacía.");
            return da.GuardarInsumosCalculados(calculados, sessionId, usuario);
        }

        public List<E_InsumoCalculado> ObtenerInsumosCalculados(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return new List<E_InsumoCalculado>();
            return da.ObtenerInsumosCalculados(sessionId);
        }

        public bool ActualizarAjustesInsumosCalculados(List<E_InsumoCalculado> calculados)
        {
            return da.ActualizarAjustesInsumosCalculados(calculados);
        }

        public List<E_InsumoCalculado> BuscarInsumosFiltro(string query)
        {
            return da.BuscarInsumosFiltro(query);
        }
    }
}
