using System;

namespace AppGenReceta.BE
{
    public class E_InsumoNP
    {
        public string NP { get; set; }
        public string COD_FABRICA { get; set; }
        public string COD_PRESENT { get; set; }
        public string COD_PROVEEDOR { get; set; }
        public string COMBO { get; set; }
        public string ESTILO_PROPIO { get; set; }
        public string ESTILO_CLIENTE { get; set; }
        public string NOM_CLIENTE { get; set; }
        public string COD_FAMITEM { get; set; }
        public string DES_FAMITEM { get; set; }
        public string PROVEEDOR { get; set; }
        public int NUM_ENPROCESO { get; set; }
        public int NUM_ENREPROCESOS { get; set; }
        public int PROCESADAS { get; set; }
        public int REQUERIDAS { get; set; }
        public int POR_INGRESAR { get; set; }
        public int NRO_ARTES { get; set; }
        public decimal STOCK_VALORIZADO { get; set; }
        public string FEC_DESPACHO { get; set; }
        public string FEC_PRIENTRADA { get; set; }
        public string FECHA_FIN_PRODUCCION { get; set; }
        public string FEC_SUGERIDA_DESPACHO { get; set; }
        public bool Seleccionado { get; set; } = true;
    }
}
