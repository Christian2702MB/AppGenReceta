using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AppGenReceta.BE
{
    public class VisitaBE
    {
        public String Cod_EstPro { get; set; }
        public String Motivo { get; set; }
        public String TimeProceso { get; set; }
        public String IDCliente { get; set; }
        public String Cliente { get; set; }
        public String Estilo { get; set; }
        public String EstiloPropio { get; set; }
        public String TemporadaNew { get; set; }
        public String Prenda { get; set; }
        public String CuerpoCorreo { get; set; }
        //Seccion Generales para Insertar
        public String ObservacionInsertarGeneral { get; set; }
        public String UsuarioInsertarGeneral { get; set; }
        [XmlIgnore]
        public Int32 IDDetalle { get; set; }
        //Seccion de nombre archivos
        public String rutaDescarga { get; set; }
        //seccion extensiones
        public String Extension { get; set; }
        public String Formato { get; set; }
        public String Tipo { get; set; }
        public String Responsable { get; set; }
        public String DifOP { get; set; }
        public String FechReq { get; set; }
        public String FechPro { get; set; }
        public String DifFech { get; set; }
        public String FechRetraso { get; set; }
        public String kilos { get; set; }
        
        //Campos Old
        [XmlIgnore]
        public Int32 IDVisita { get; set; }
        [XmlIgnore]
        public Int32 IDDatosCompra { get; set; }
        public String Colaborador { get; set; }
        public String Fecha { get; set; }
        public String Estado { get; set; }
        //ingreso y salida de vigilancia
        [XmlIgnore]
        public DateTime FechaIngresoSeguridad { get; set; }
        [XmlIgnore]
        public DateTime FechaSalidaSeguridad { get; set; }
        public String DiaSemana { get; set; }

        //Ejecutor indica quien realiza la acción
        public String Ejecutor { get; set; }

        //cmendez
        //28-09-25
        public String Dato { get; set; }

        //03/03/26
        public List<NPBE> NPS { get; set; }

        //para generar niveles
        public String NP { get; set; }

        public string FechaUDP { get; set; } // Puedes enviarlo como string o DateTime
        public string Operario { get; set; }
        public string Tecnica { get; set; }
       
        public string ComboCabecera { get; set; }
        public string PrendasReq { get; set; }
        public string Ubicacion { get; set; }

        public string Arte { get; set; }
        public Int32 hdnIdVisita { get; set; }

        //cmendez
        //28-09-25
        // En VisitaBE.cs
        public string UsuarioCierre { get; set; }
        public string FechaCierre { get; set; }
        public string UsuarioApertura { get; set; }
        public string FechaApertura { get; set; }
        public bool EstaCerrado { get; set; } // Flag para saber el estado actual

        public bool IsReadOnly { get; set; } // Para controlar el estado en la vista

        // Dentro de la clase VisitaBE, verifica o agrega estas propiedades:
        public string Item { get; set; }
        public string Temporada { get; set; }

        public string FechaRegistro { get; set; }

        public string HoraRegistro { get; set; }

        public string Observaciones { get; set; }

        [XmlArray("Colores")]
        [XmlArrayItem("ColorBE")]
        public List<ColorBE> Colores { get; set; }

        public VisitaBE()
        {
            Colores = new List<ColorBE>();
        }
    }

    [Serializable]
    public class ColorBE
    {

        public int IdColor { get; set; }
        public string CodigoColor { get; set; }
        public string NombreColor { get; set; }
        public string Nombre { get; set; }
        public string Combo { get; set; }

        [XmlArray("Insumos")]
        [XmlArrayItem("InsumoBE")]
        public List<InsumoBE> Insumos { get; set; }

        public ColorBE()
        {
            Insumos = new List<InsumoBE>();
        }
    }

    //Iniciao 01/04/26
    [Serializable]
    public class PruebaUDPBE
    {
        public int IdPrueba { get; set; }
        public string NombrePrueba { get; set; }
        public double GramosUDP { get; set; }
        public bool EsPrincipal { get; set; }
    }

    // Extensión de la clase existente en VisitaBE.cs
    [Serializable]
    public class InsumoBE
    {
        public int IdInsumo { get; set; }
        public string Codigo { get; set; }
        public decimal Cantidad { get; set; }
        public string CodigoInsumo { get; set; }
        public string Descripcion { get; set; }
        public double GramosUDP { get; set; } // Conservará el Valor Original
        public double ConsumoUDP { get; set; }

        // Nueva lista para almacenar el historial de pruebas
        [XmlArray("PruebasUDP")] 
        [XmlArrayItem("PruebaUDPBE")]
        public List<PruebaUDPBE> Pruebas { get; set; }

        public InsumoBE()
        {
            Pruebas = new List<PruebaUDPBE>();
        }
    }

    //Fin

    [Serializable]
    public class ItemBE
    {
        public string CodItem { get; set; }
        public string NombreItem { get; set; }
    }

    [Serializable]
    public class EstiloBE
    {
        // Relación 1 a 1 requerida por tu JavaScript
        public string CodEstiloCliente { get; set; }
        public string NombreEstiloCliente { get; set; }
        public string CodEstiloPropio { get; set; }
        public string NombreEstiloPropio { get; set; }
    }
}
