using System.Collections.Generic;

namespace AppGenReceta.BE
{
    /// <summary>
    /// ViewModel para la pantalla Paso 4: Generar Solicitud de Requerimiento.
    /// Contiene la cabecera, el resumen de compra y los ítems a cargar.
    /// </summary>
    public class E_SolicitudResumen
    {
        // ── Identificación de la sesión ──────────────────────
        public string SessionId { get; set; }

        // ── Cabecera de solicitud ────────────────────────────
        public string Motivo          { get; set; } = "COMPRA";
        public string Observacion     { get; set; } = ""; // Editable por el usuario
        public List<string> NPsIncluidas { get; set; } = new List<string>();
        public string ModuloDestino   { get; set; } = "Gestión de Pedidos";
        public string Ruta            { get; set; } = "Movimientos > Repuestos > Solicitud";

        // ── Resumen de compra ─────────────────────────────────
        /// <summary>Insumos donde TIPO_DESPACHO != 'Vacio'</summary>
        public int ItemsAComprar { get; set; }

        /// <summary>Insumos donde STOCK_CONSULTADO > 0</summary>
        public int ItemsConStock { get; set; }

        /// <summary>SUM(CANTIDAD_A_PEDIR * PRECIO_UNITARIO)</summary>
        public decimal CostoEstimado { get; set; }

        /// <summary>COUNT(DISTINCT ID_PROVEEDOR_ASIGNADO) donde ID > 0</summary>
        public int CantidadProveedores { get; set; }

        // ── Ítems para la tabla inferior ─────────────────────
        /// <summary>Solo insumos con CANTIDAD_A_PEDIR > 0 (requieren compra)</summary>
        public List<E_InsumoCalculado> Items { get; set; } = new List<E_InsumoCalculado>();

        public string NomTrabajador { get; set; } = ""; // Nombre del trabajador
        public string Trabajador { get; set; } = ""; // Codigo del trabajador
    }
}
