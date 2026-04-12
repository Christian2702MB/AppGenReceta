using System;

namespace AppGenReceta.BE
{
    public class ProveedorBE
    {
        public Int32 IDProveedor { get; set; }
        public String Nombres { get; set; }
        public String ApellidoPaterno { get; set; }
        public String ApellidoMaterno { get; set; }
        public String Documento { get; set; }
        public String Correo { get; set; }
        public String Telefono { get; set; }
        public Boolean Activo { get; set; }
        public String Contrasena { get; set; }
        public String RUC { get; set; }
        public String RazonSocial { get; set; }
        public String TelefonoEmpresa { get; set; }
        public String Cliente { get; set; }

        //Agregar si se desea poner contador
        //public Int32 NumCambioClave { get; set; }
        //agregar
        //public String Clave { get; set; }

        public Int32 IDUsuario { get; set; }
        public int IdRol { get; set; } // 1: Visualizador, 2: Editor, 3: Administrador
        public string NombreRol { get; set; } = String.Empty;
    }
}
