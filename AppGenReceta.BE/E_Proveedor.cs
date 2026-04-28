using System;

namespace AppGenReceta.BE
{
    public class E_Proveedor
    {
        public int IdProveedor { get; set; }
        public string Ruc { get; set; }
        public string RazonSocial { get; set; }
        public string Contacto { get; set; }
        public bool EsActivo { get; set; }
        public DateTime FechaRegistro { get; set; }
        
        // Relación Maestro-Detalle
        public System.Collections.Generic.List<E_InsumoProveedor> ListaInsumos { get; set; }
    }

    public class E_InsumoProveedor
    {
        public int IdRelacion { get; set; }
        public int IdProveedor { get; set; }
        
        // Datos del Proveedor Cruzado
        public string RazonSocial { get; set; }
        
        // Datos del Insumo Cruzado
        public string CodigoArticulo { get; set; }
        public string DescripcionInsumo { get; set; } 
        
        public decimal CapacidadNumerica { get; set; } // Ej: 20
        public string DescripcionPresentacion { get; set; } // Ej: Balde 20 kg
        public string UnidadMedida { get; set; } // Ej: kg
        public decimal PrecioUnitario { get; set; } // Precio por KG o Ltr
        
        // Calculado
        public decimal PrecioPresentacion 
        { 
            get { return CapacidadNumerica * PrecioUnitario; } 
        }
    }
}
