using System;

namespace AppGenReceta.BE
{
    public class E_VoucherDetalle
    {
        public string NumRequerimiento { get; set; }
        public string Fecha { get; set; }
        public string NomArea { get; set; }
        public string Trabajador { get; set; }
        public string NomTrabajador { get; set; }
        public string DesMotivo { get; set; }
        public string Observacion { get; set; }
        public int Secuencia { get; set; }
        public string CodItem { get; set; }
        public string CodRepuesto { get; set; }
        public string DesItem { get; set; }
        public string CodFabricacion { get; set; }
        public decimal Cantidad { get; set; }
        public string DesUniMed { get; set; }
        public string UltimosPrecios { get; set; }
    }
}
