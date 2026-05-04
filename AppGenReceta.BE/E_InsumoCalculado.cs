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

        /// <summary>Precio unitario de la presentación (para costo estimado en Paso 4)</summary>
        public decimal PrecioUnitario { get; set; }
    }

    // ─────────────────────────────────────────────────────────
    // Insumo calculado enriquecido con stock y proveedores
    // ─────────────────────────────────────────────────────────
    public class E_InsumoCalculado
    {
        public int ID_CALCULO { get; set; }

        // ── Campos del cálculo de insumos ────────────────────
        public string CodigoInsumo { get; set; }
        public string Descripcion  { get; set; }
        public string NombreColor  { get; set; }
        public string NombrePrueba { get; set; }
        public decimal GramosUDP   { get; set; }

        // ── Cantidad sugerida (original antes de edición) ────
        public decimal GramosUDPSugerido { get; set; }

        // ── Stock real (USP_EST_OBTENER_STOCK_ACTUAL) ─────────
        public decimal StockActual { get; set; }

        // ── Presentación y capacidad (proveedor seleccionado) ─
        /// <summary>Texto: DES_PRESENTACION + CAPACIDAD_NUMERICA + UNIDAD_MEDIDA → "Balde 20 kg"</summary>
        public string Presentacion { get; set; }

        /// <summary>Capacidad del proveedor/presentación principal (divisor para envases)</summary>
        public decimal CapacidadNumerica { get; set; }

        // ── Proveedores relacionados al insumo ───────────────
        /// <summary>
        /// Lista tipada de proveedores para este COD_ARTICULO.
        /// Vacía → combo muestra solo "Stock propio".
        /// </summary>
        public List<E_ProveedorOpcion> ProveedoresOpciones { get; set; }
            = new List<E_ProveedorOpcion>();

        // ── Resultado de la elección del usuario ─────────────
        public decimal CantidadAPedir      { get; set; }
        public string  TipoDespacho        { get; set; }
        public int     IdProveedorAsignado { get; set; }

        // ── Precio unitario (para resumen Paso 4) ────────────
        public decimal PrecioUnitario { get; set; }
        public string  UnidadMedida   { get; set; }

        // ── Marca insumo agregado manualmente ────────────────
        /// <summary>true = agregado con botón "Insumo extra-receta" (no viene del cálculo)</summary>
        public bool EsExtraReceta { get; set; }

        // ── Auditoría ────────────────────────────────────────
        public string USUARIO_PROCESA { get; set; }
        public string SESSION_ID      { get; set; }
    }
}
