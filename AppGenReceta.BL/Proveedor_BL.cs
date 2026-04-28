using AppGenReceta.BE;
using AppGenReceta.DA;
using System.Collections.Generic;

namespace AppGenReceta.BL
{
    public class Proveedor_BL
    {
        private Proveedor_DA da = new Proveedor_DA();

        public List<E_InsumoProveedor> ListarRelacionesProveedorInsumo(string filtroTexto)
        {
            return da.ListarRelacionesProveedorInsumo(filtroTexto);
        }

        public int GuardarProveedorConInsumos(E_Proveedor proveedor)
        {
            return da.GuardarProveedorConInsumos(proveedor);
        }

        public bool EliminarRelacion(int idRelacion)
        {
            return da.EliminarRelacion(idRelacion);
        }

        public E_Proveedor ObtenerProveedorConDetalles(int idProveedor)
        {
            return da.ObtenerProveedorConDetalles(idProveedor);
        }
    }
}
