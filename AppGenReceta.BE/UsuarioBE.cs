using System;

namespace AppGenReceta.BE
{
    public class UsuarioBE
    {
        public String NombreUsuario { get; set; }
        public String Clave { get; set; }
        public String ClaveNew { get; set; }
        public String ClaveVerificar { get; set; }

        public Int32 IDUsuario { get; set; }
        public String Nombres { get; set; }
        public String ApellidoPaterno { get; set; }
        public String ApellidoMaterno { get; set; }
        public String Documento { get; set; }
        public String Correo { get; set; }
        public String Telefono { get; set; }
        public String Contrasena { get; set; }

        public int IdRol { get; set; } // 1: Visualizador, 2: Editor, 3: Administrador
        public string NombreRol { get; set; } = String.Empty;
    }
}
