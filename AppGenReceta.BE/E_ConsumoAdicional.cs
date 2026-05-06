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

        // Reporte
        public string Maquina { get; set; }
        public string OP { get; set; }
        public string Cliente { get; set; }
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

    /// <summary>
    /// Mapea el resultado de Ti_sm_muestra_Voucher para el Voucher de QYC.
    /// Cada fila del SP es una línea de detalle; los campos de cabecera se repiten.
    /// </summary>
    public class E_VoucherQyc
    {
        // Cabecera
        public int    NumRequerimiento { get; set; }
        public string Partida          { get; set; }  // CASE WHEN cod_ordtra in ('','0') → Cod_Ordtra
        public string Maquina          { get; set; }  // Des_Maquina_Tinto
        public string Motivo           { get; set; }  // D.Descripcion AS Motivo
        public string Fecha            { get; set; }  // Fec_Creacion
        public string Observaciones    { get; set; }
        public string OP               { get; set; }  // COD_ORDPRO_TEX
        public string Cliente          { get; set; }  // Nom_Cliente
        public string KgsCrudo         { get; set; }  // Kgs_Crudo (ej: "45.00 kgs")

        // Detalle
        public int     Secuencia    { get; set; }  // índice generado (SP no devuelve Secu)
        public string  CodItem      { get; set; }  // Cod_Item
        public string  DesItem      { get; set; }  // Des_Item AS Descripcion
        public string  UnidadMedida { get; set; }  // SP no devuelve UM; se deja vacío
        public decimal Cantidad     { get; set; }  // Cons_Requerido
        public int     Lote         { get; set; }  // Lote
    }
}
