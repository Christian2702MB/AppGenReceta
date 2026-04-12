using AppGenReceta.BE;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AppGenReceta.DA
{
    public class EmpleadoDA
    {
        private String ConnectionString = ConfigurationManager.ConnectionStrings["AppGenReceta_SQL"].ConnectionString;

        public int Registrar(EmpleadoBE empleado)
        {
            int Id = 0;
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.USP_Empleado_Registrar", cnx);

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 600;

                    cmd.Parameters.AddWithValue("@Nombres", empleado.Nombres);
                    cmd.Parameters.AddWithValue("@Correo", empleado.Correo);
                    cmd.Parameters.AddWithValue("@IDTipoEmpleado", empleado.IDTipoEmpleado);
                    

                    cnx.Open();

                    Id = int.Parse(cmd.ExecuteScalar().ToString());

                    cnx.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Id;
        }

        public int ActualizarNombre(EmpleadoBE empleado)
        {
            int Id = 0;
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.USP_Empleado_ActualizarNombre", cnx);

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 600;

                    cmd.Parameters.AddWithValue("@Nombres", empleado.Nombres);
                    cmd.Parameters.AddWithValue("@IDEmpleado", empleado.IDEmpleado);


                    cnx.Open();

                    Id = int.Parse(cmd.ExecuteScalar().ToString());

                    cnx.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Id;
        }

        public EmpleadoBE ObtenerEmpleadoPorCorreo(String Correo)
        {
            EmpleadoBE item;
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.USP_Empleado_ObtenerPorCorreo", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@Correo", Correo);

                    cnx.Open();
                    IDataReader dr = cmd.ExecuteReader();
                    item = null;
                    while (dr.Read())
                    {
                        item = new EmpleadoBE();
                        if (!dr.IsDBNull(dr.GetOrdinal("IDEmpleado")))
                        {
                            item.IDEmpleado = dr.GetInt32(dr.GetOrdinal("IDEmpleado"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("Nombres")))
                        {
                            item.Nombres = dr.GetString(dr.GetOrdinal("Nombres"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("Correo")))
                        {
                            item.Correo = dr.GetString(dr.GetOrdinal("Correo"));
                        }

                        if (!dr.IsDBNull(dr.GetOrdinal("IDTipoEmpleado")))
                        {
                            item.IDTipoEmpleado = dr.GetInt32(dr.GetOrdinal("IDTipoEmpleado"));
                        }
                    }
                    cnx.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return item;
        }

    }
}
