using System.Collections.Generic;

namespace AppGenReceta.BE
{
    // ─────────────────────────────────────────────────────────
    // Opción de proveedor para el dropdown por fila de insumo
    // ─────────────────────────────────────────────────────────
    public class E_ProveedorOpcion
    {
        /// <summary>PK de TBL_ESTAMPADO_PROVEEDOR</summary>
        public int IdProveedor { get; set; }

        /// <summary>Razón social a mostrar en el combo</summary>
        public string RazonSocial { get; set; }

        /// <summary>Presentación: des_presentacion + capacidad_numerica + unidad_medida → "Balde 20 kg"</summary>
        public string Presentacion { get; set; }

        /// <summary>Capacidad numérica del envase (divisor matemático para cant. a pedir)</summary>
        public decimal CapacidadNumerica { get; set; }
    }

    // ─────────────────────────────────────────────────────────
    // Insumo calculado enriquecido con stock y proveedores
    // ─────────────────────────────────────────────────────────
    public class E_InsumoCalculado
    {
        public int ID_CALCULO { get; set; }

        // ── Campos del SP GRI_CALCULAR_INSUMOS ──────────────
        public string CodigoInsumo { get; set; }
        public string Descripcion  { get; set; }
        public string NombreColor  { get; set; }
        public string NombrePrueba { get; set; }
        public decimal GramosUDP   { get; set; }

        // ── Cantidad sugerida (original, no editada) ─────────
        /// <summary>Valor calculado original antes de que el usuario lo edite</summary>
        public decimal GramosUDPSugerido { get; set; }

        // ── Stock real (desde USP_EST_OBTENER_STOCK_ACTUAL) ──
        public decimal StockActual { get; set; }

        // ── Presentación del primer proveedor ───────────────
        /// <summary>
        /// Texto: des_presentacion + ' ' + capacidad_numerica + ' ' + unidad_medida
        /// Ej.: "Balde 20 kg"
        /// </summary>
        public string Presentacion { get; set; }

        /// <summary>Capacidad numérica del proveedor principal (divisor para calcular envases)</summary>
        public decimal CapacidadNumerica { get; set; }

        // ── Proveedores relacionados al insumo ───────────────
        /// <summary>
        /// Lista de proveedores que tienen TBL_ESTAMPADO_INSUMO_PROVEEDOR para este COD_ARTICULO.
        /// Vacía → se muestra "Stock propio" en el combo.
        /// </summary>
        public List<E_ProveedorOpcion> ProveedoresOpciones { get; set; }
            = new List<E_ProveedorOpcion>();

        // ── Resultado de la elección del usuario ─────────────
        public decimal CantidadAPedir      { get; set; }
        public string  TipoDespacho        { get; set; }
        public int     IdProveedorAsignado { get; set; }

        // ── Auditoría ────────────────────────────────────────
        public string USUARIO_PROCESA { get; set; }
        public string SESSION_ID      { get; set; }
    }
}
