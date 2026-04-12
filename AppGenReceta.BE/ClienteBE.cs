using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGenReceta.BE
{
    public class ClienteBE
    {
        public Int32 IDCliente { get; set; }
        public String Siglas { get; set; }
        public String Nombres { get; set; }
        public String ApellidoPaterno { get; set; }
        public String ApellidoMaterno { get; set; }
        public String Documento { get; set; }
        public String Correo { get; set; }
        public String Telefono { get; set; }
        public Boolean Activo { get; set; }
        public String Contrasena { get; set; }
        public String Cliente { get; set; }

        public String Area { get; set; }
        public List<ClienteBE> Clientes { get; set; }

        public List<ClienteBE> Areas { get; set; }

    }
}
