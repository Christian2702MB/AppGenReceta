using AppGenReceta.BE;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

//using SP = Microsoft.SharePoint;

namespace AppGenReceta.DA
{
    public class UsuarioDA
    {
        private String ConnectionString = ConfigurationManager.ConnectionStrings["AppGenReceta_SQL"].ConnectionString;
        private String ConnectionStringPrueba = ConfigurationManager.ConnectionStrings["AppGenReceta_SQL_Prueba"].ConnectionString;

        public UsuarioBE ValidarUsuario(UsuarioBE libro)
        {
            UsuarioBE item;
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.AGO_Validar_Usuario", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@Usuario", libro.NombreUsuario);
                    cmd.Parameters.AddWithValue("@Clave", libro.Clave);
                    //cmd.Parameters.AddWithValue("@Clave", EncriptarBase64(libro.Clave));

                    cnx.Open();
                    IDataReader dr = cmd.ExecuteReader();
                    item = null;
                    while (dr.Read())
                    {
                        item = new UsuarioBE();
                        if (!dr.IsDBNull(dr.GetOrdinal("Nombres")))
                        {
                            item.Nombres = dr.GetString(dr.GetOrdinal("Nombres"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("ApellidoPaterno")))
                        {
                            item.ApellidoPaterno = dr.GetString(dr.GetOrdinal("ApellidoPaterno"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("ApellidoMaterno")))
                        {
                            item.ApellidoMaterno = dr.GetString(dr.GetOrdinal("ApellidoMaterno"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("Documento")))
                        {
                            item.Documento = dr.GetString(dr.GetOrdinal("Documento"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("Correo")))
                        {
                            item.Correo = dr.GetString(dr.GetOrdinal("Correo"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("NombreRol")))
                        {
                            item.NombreRol = dr.GetString(dr.GetOrdinal("NombreRol"));
                        }
                        //Agregado cmendez 11/05/26
                        if (!dr.IsDBNull(dr.GetOrdinal("Cod_Usuario")))
                        {
                            item.Cod_Usuario = dr.GetString(dr.GetOrdinal("Cod_Usuario"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("Cod_Fabrica")))
                        {
                            item.Cod_Fabrica = dr.GetString(dr.GetOrdinal("Cod_Fabrica"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("Tip_Trabajador")))
                        {
                            item.Tip_Trabajador = dr.GetString(dr.GetOrdinal("Tip_Trabajador"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("Cod_Trabajador")))
                        {
                            item.Cod_Trabajador = dr.GetString(dr.GetOrdinal("Cod_Trabajador"));
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

        public String ConcatenarCorreos(String area)
        {
            UsuarioBE item;
            String cadena = "";
            String Resultado;
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.AGO_Concatenar_Usuario", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@Area", area);
                    cnx.Open();
                    IDataReader dr = cmd.ExecuteReader();
                    item = null;
                    while (dr.Read())
                    {
                        item = new UsuarioBE();
                        if (!dr.IsDBNull(dr.GetOrdinal("Correo")))
                        {
                            item.Correo = dr.GetString(dr.GetOrdinal("Correo"));
                            cadena = cadena + item.Correo + ";";
                        }
                    }
                    cnx.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            if (cadena.Length <= 1)
            {
                Resultado = "";
            }                
            else
            {
                Resultado = cadena.Substring(0, cadena.Length - 1);
            }
            return Resultado;
        }
            
    }
}
