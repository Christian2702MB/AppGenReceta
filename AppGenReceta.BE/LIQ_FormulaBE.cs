using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AppGenReceta.BE
{
    /// <summary>
    /// Entidad principal de Fórmula para el módulo de Liquidación de Insumos.
    /// Refleja la estructura de LIQ_Formulas en BD.
    /// </summary>
    [Serializable]
    [XmlRoot("FormulaBE")]
    public class LIQ_FormulaBE
    {
        public int IdFormula { get; set; }
        public int IdRecetaOrigen { get; set; }

        // Datos de cabecera (copiados de la receta origen)
        public string NP { get; set; }
        public string Cliente { get; set; }
        public string Temporada { get; set; }
        public string Estilo { get; set; }
        public string EstiloPropio { get; set; }
        public string Item { get; set; }
        public string ComboCabecera { get; set; }
        public string Ubicacion { get; set; }
        public string Tecnica { get; set; }
        public string Operario { get; set; }
        public string FechaUDP { get; set; }
        public string PrendasReq { get; set; }
        public string Arte { get; set; }

        // Estado y auditoría
        public string Estado { get; set; }
        public string FechaRegistro { get; set; }
        public string HoraRegistro { get; set; }
        public string UsuarioCierre { get; set; }
        public string FechaCierre { get; set; }
        public string UsuarioApertura { get; set; }
        public string FechaApertura { get; set; }
        public string UsuarioModificacion { get; set; }

        // Para control de vista
        public bool EstaCerrado { get; set; }
        public bool IsReadOnly { get; set; }

        // Detalle: Colores e Insumos
        [XmlArray("Colores")]
        [XmlArrayItem("ColorBE")]
        public List<LIQ_FormulaColorBE> Colores { get; set; }

        public LIQ_FormulaBE()
        {
            Colores = new List<LIQ_FormulaColorBE>();
        }
    }

    /// <summary>
    /// Color Pantone dentro de una fórmula de liquidación.
    /// </summary>
    [Serializable]
    public class LIQ_FormulaColorBE
    {
        public int IdColor { get; set; }
        public string NombreColor { get; set; }
        public string Nombre { get; set; } // Alias para serialización XML
        public string Combo { get; set; }

        [XmlArray("Insumos")]
        [XmlArrayItem("InsumoBE")]
        public List<LIQ_FormulaInsumoBE> Insumos { get; set; }

        public LIQ_FormulaColorBE()
        {
            Insumos = new List<LIQ_FormulaInsumoBE>();
        }
    }

    /// <summary>
    /// Insumo dentro de un color de fórmula de liquidación.
    /// </summary>
    [Serializable]
    public class LIQ_FormulaInsumoBE
    {
        public int IdInsumo { get; set; }
        public string CodigoInsumo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }

        // Pruebas UDP
        [XmlArray("PruebasUDP")]
        [XmlArrayItem("PruebaUDPBE")]
        public List<LIQ_FormulaPruebaBE> Pruebas { get; set; }

        public LIQ_FormulaInsumoBE()
        {
            Pruebas = new List<LIQ_FormulaPruebaBE>();
        }
    }

    /// <summary>
    /// Prueba UDP dentro de un insumo de fórmula.
    /// </summary>
    [Serializable]
    public class LIQ_FormulaPruebaBE
    {
        public int IdPrueba { get; set; }
        public string NombrePrueba { get; set; }
        public double GramosUDP { get; set; }
        public bool EsPrincipal { get; set; }
    }

    /// <summary>
    /// DTO para la lista de recetas disponibles para crear fórmulas.
    /// </summary>
    [Serializable]
    public class LIQ_RecetaDisponibleBE
    {
        public int IdRecetas { get; set; }
        public string NP { get; set; }
        public string Cliente { get; set; }
        public string Temporada { get; set; }
        public string Estilo { get; set; }
        public string EstiloPropio { get; set; }
        public string Item { get; set; }
        public string Combo { get; set; }
        public string Ubicacion { get; set; }
        public string Tecnica { get; set; }
        public string OperarioUDP { get; set; }
        public string FechaRegistro { get; set; }
    }
}
