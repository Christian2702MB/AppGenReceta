using System;
using System.Collections.Generic;

namespace AppGenReceta.BE
{
    /// <summary>
    /// Entidad que representa una Operación (Consumo, Merma, Devolucion, Ingreso)
    /// </summary>
    [Serializable]
    public class LIQ_OperacionBE
    {
        public string NP { get; set; }
        public string CodInsumo { get; set; }
        public string TipoOperacion { get; set; }
        public decimal Cantidad { get; set; }
        public string Motivo { get; set; }
        public string MermaReutilizada { get; set; }
        public string Usuario { get; set; }
        public string FuenteConsumo { get; set; }
    }

    /// <summary>
    /// Entidad para representar una Merma Reutilizable en Stock
    /// </summary>
    [Serializable]
    public class LIQ_MermaStockBE
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string Tecnica { get; set; }
        public string NPOrigen { get; set; }
        public string FechaGeneracion { get; set; }
        public string FechaVencimiento { get; set; }
        public decimal CantidadDisponible { get; set; }
        public string UM { get; set; }
        public string Estado { get; set; }
    }

    /// <summary>
    /// Entidades de lectura (DTO) estructuradas para enviar JSON agrupado a la Vista
    /// </summary>
    [Serializable]
    public class LIQ_LiquidacionConsolidadaBE
    {
        public string NP { get; set; }
        public string Cliente { get; set; }
        public string Estilo { get; set; }
        public string Combo { get; set; }
        public string Estado { get; set; }
        public string Creacion { get; set; }
        public string Cierre { get; set; }
        public List<LIQ_LiquidacionColorBE> Colores { get; set; }

        public LIQ_LiquidacionConsolidadaBE()
        {
            Colores = new List<LIQ_LiquidacionColorBE>();
        }
    }

    [Serializable]
    public class LIQ_LiquidacionColorBE
    {
        public string Pantone { get; set; }
        public List<LIQ_LiquidacionInsumoBE> Insumos { get; set; }

        public LIQ_LiquidacionColorBE()
        {
            Insumos = new List<LIQ_LiquidacionInsumoBE>();
        }
    }

    [Serializable]
    public class LIQ_LiquidacionInsumoBE
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Tecnica { get; set; }
        public string UM { get; set; }
        public decimal Requerido { get; set; }
        public decimal Consumido { get; set; }
        public decimal ConsumidoInicial { get; set; }
        public decimal ConsumidoSolicitud { get; set; }
        public decimal Devuelto { get; set; }
        public decimal Merma { get; set; }
        public decimal Saldo { get; set; }
        public string LoteVenc { get; set; }
        public string Trazabilidad { get; set; }
    }

    [Serializable]
    public class LIQ_SaldosPopupBE
    {
        public decimal StockInicial { get; set; }
        public decimal StockSolicitud { get; set; }
        public decimal StockTotal { get; set; }
    }
}
