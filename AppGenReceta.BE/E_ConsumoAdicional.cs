using System;
using System.Collections.Generic;

namespace AppGenReceta.BE
{
    public class E_ConsumoAdicionalCabecera
    {
        public int NumRequerimiento { get; set; }
        public string FecCreacion { get; set; }
        public string UltimaImpresion { get; set; }
        public string Partida { get; set; } // COD_ORDTRA
        public string Motivo { get; set; }
        public string Nombre { get; set; } // Nombre primer insumo o máquina (visual)
        public string Unidad { get; set; }

        // Propiedades adicionales para la Inserción
        public string CodMotivo { get; set; }
        public string CodMaquina { get; set; }
        public string Observaciones { get; set; }
    }

    public class E_ConsumoAdicionalDetalle
    {
        public int NumRequerimiento { get; set; }
        public int Secuencia { get; set; }
        public string CodItem { get; set; }
        public string Nombre { get; set; }
        public decimal ConsumoRequerido { get; set; }
        public string Partida { get; set; }
        public int Lote { get; set; }
        public string Unidad { get; set; }
    }
}
