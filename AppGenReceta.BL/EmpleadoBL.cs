using AppGenReceta.BE;
using AppGenReceta.DA;
using System;
using System.Collections.Generic;

namespace AppGenReceta.BL
{
    public class EmpleadoBL
    {
        public EmpleadoBE ObtenerEmpleadoPorCorreo(String Correo)
        {
            EmpleadoDA empleadoDA = new EmpleadoDA();
            try
            {
                return empleadoDA.ObtenerEmpleadoPorCorreo(Correo);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int Registrar(EmpleadoBE empleado)
        {
            EmpleadoDA empleadoDA = new EmpleadoDA();
            try
            {
                return empleadoDA.Registrar(empleado);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int ActualizarNombre(EmpleadoBE empleado)
        {
            EmpleadoDA empleadoDA = new EmpleadoDA();
            try
            {
                return empleadoDA.ActualizarNombre(empleado);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
