using AppGenReceta.BE;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AppGenReceta.DA
{
    public class Proveedor_DA
    {
        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["AppGenReceta_SQL"].ConnectionString;
        }

        private string GetStringSafe(SqlDataReader dr, string columnName)
        {
            int ordinal = dr.GetOrdinal(columnName);
            return dr.IsDBNull(ordinal) ? string.Empty : dr.GetString(ordinal);
        }

        private decimal GetDecimalSafe(SqlDataReader dr, string columnName)
        {
            int ordinal = dr.GetOrdinal(columnName);
            // Handle different numerical casts safely
            if (dr.IsDBNull(ordinal)) return 0;
            var val = dr.GetValue(ordinal);
            return Convert.ToDecimal(val);
        }

        private int GetIntSafe(SqlDataReader dr, string columnName)
        {
            int ordinal = dr.GetOrdinal(columnName);
            return dr.IsDBNull(ordinal) ? 0 : Convert.ToInt32(dr.GetValue(ordinal));
        }

        public List<E_InsumoProveedor> ListarRelacionesProveedorInsumo(string filtroTexto)
        {
            List<E_InsumoProveedor> lista = new List<E_InsumoProveedor>();

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand("USP_EST_LISTAR_PROVEEDORES_INSUMOS", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@FiltROTexto", string.IsNullOrEmpty(filtroTexto) ? "" : filtroTexto);

                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            E_InsumoProveedor e = new E_InsumoProveedor();
                            e.IdRelacion = GetIntSafe(dr, "ID_RELACION");
                            e.IdProveedor = GetIntSafe(dr, "ID_PROVEEDOR");
                            e.RazonSocial = GetStringSafe(dr, "RAZON_SOCIAL");
                            e.CodigoArticulo = GetStringSafe(dr, "COD_ARTICULO");
                            e.DescripcionInsumo = GetStringSafe(dr, "DES_INSUMO");
                            e.CapacidadNumerica = GetDecimalSafe(dr, "CAPACIDAD_NUMERICA");
                            e.DescripcionPresentacion = GetStringSafe(dr, "DES_PRESENTACION");
                            e.UnidadMedida = GetStringSafe(dr, "UNIDAD_MEDIDA");
                            e.PrecioUnitario = GetDecimalSafe(dr, "PRECIO_UNITARIO");

                            lista.Add(e);
                        }
                    }
                }
            }
            return lista;
        }
        public int GuardarProveedorConInsumos(E_Proveedor cabecera)
        {
            int idProveedorGenerado = 0;

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();
                using (SqlTransaction trx = con.BeginTransaction())
                {
                    try
                    {
                        // 1. Guardar o Recuperar Proveedor Cabecera
                        using (SqlCommand cmdCab = new SqlCommand("USP_EST_REGISTRAR_PROVEEDOR", con, trx))
                        {
                            cmdCab.CommandType = CommandType.StoredProcedure;
                            cmdCab.Parameters.AddWithValue("@RUC", cabecera.Ruc ?? string.Empty);
                            cmdCab.Parameters.AddWithValue("@RAZON_SOCIAL", cabecera.RazonSocial ?? string.Empty);
                            cmdCab.Parameters.AddWithValue("@CONTACTO", cabecera.Contacto ?? string.Empty);

                            object resultId = cmdCab.ExecuteScalar();
                            if (resultId != null && resultId != DBNull.Value)
                            {
                                idProveedorGenerado = Convert.ToInt32(resultId);
                            }
                        }

                        // 2. Si es edición explícita, limpiamos la grilla vieja primero
                        if (cabecera.IdProveedor > 0)
                        {
                            using (SqlCommand cmdDel = new SqlCommand("DELETE FROM [dbo].[TBL_ESTAMPADO_INSUMO_PROVEEDOR] WHERE ID_PROVEEDOR = @ID_PROVEEDOR", con, trx))
                            {
                                cmdDel.Parameters.AddWithValue("@ID_PROVEEDOR", idProveedorGenerado);
                                cmdDel.ExecuteNonQuery();
                            }
                        }

                        // 3. Si hay id de proveedor, guardar cada Insumo Relacionado
                        if (idProveedorGenerado > 0 && cabecera.ListaInsumos != null && cabecera.ListaInsumos.Count > 0)
                        {
                            foreach (var detalle in cabecera.ListaInsumos)
                            {
                                using (SqlCommand cmdDet = new SqlCommand("USP_EST_REGISTRAR_INSUMO_PROVEEDOR_DETALLE", con, trx))
                                {
                                    cmdDet.CommandType = CommandType.StoredProcedure;
                                    cmdDet.Parameters.AddWithValue("@ID_PROVEEDOR", idProveedorGenerado);
                                    cmdDet.Parameters.AddWithValue("@COD_ARTICULO", detalle.CodigoArticulo ?? string.Empty);
                                    cmdDet.Parameters.AddWithValue("@PRESENTACION", detalle.DescripcionPresentacion ?? string.Empty);
                                    cmdDet.Parameters.AddWithValue("@CAPACIDAD_NUMERICA", detalle.CapacidadNumerica);
                                    cmdDet.Parameters.AddWithValue("@UNIDAD_MEDIDA", detalle.UnidadMedida ?? string.Empty);
                                    cmdDet.Parameters.AddWithValue("@PRECIO_UNITARIO", detalle.PrecioUnitario);

                                    cmdDet.ExecuteNonQuery();
                                }
                            }
                        }

                        // 3. Confirmar Transaction
                        trx.Commit();
                        return idProveedorGenerado;
                    }
                    catch (Exception ex)
                    {
                        trx.Rollback();
                        throw new Exception("Error al guardar Maestro-Detalle Proveedor: " + ex.Message);
                    }
                }
            }
        }

        public bool EliminarRelacion(int idRelacion)
        {
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand("DELETE FROM [dbo].[TBL_ESTAMPADO_INSUMO_PROVEEDOR] WHERE ID_RELACION = @ID", con))
                {
                    cmd.Parameters.AddWithValue("@ID", idRelacion);
                    con.Open();
                    int r = cmd.ExecuteNonQuery();
                    return r > 0;
                }
            }
        }

        public E_Proveedor ObtenerProveedorConDetalles(int idProveedor)
        {
            E_Proveedor proveedor = new E_Proveedor();
            proveedor.ListaInsumos = new List<E_InsumoProveedor>();

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();
                
                // 1. Obtener Cabecera
                using (SqlCommand cmd = new SqlCommand("SELECT ID_PROVEEDOR, RUC, RAZON_SOCIAL, CONTACTO FROM [dbo].[TBL_ESTAMPADO_PROVEEDOR] WHERE ID_PROVEEDOR = @ID", con))
                {
                    cmd.Parameters.AddWithValue("@ID", idProveedor);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            proveedor.IdProveedor = GetIntSafe(dr, "ID_PROVEEDOR");
                            proveedor.Ruc = GetStringSafe(dr, "RUC");
                            proveedor.RazonSocial = GetStringSafe(dr, "RAZON_SOCIAL");
                            proveedor.Contacto = GetStringSafe(dr, "CONTACTO");
                        }
                    }
                }

                if(proveedor.IdProveedor == 0) return null;

                // 2. Obtener Lista
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ID_RELACION, ID_PROVEEDOR, COD_ARTICULO, DES_PRESENTACION, CAPACIDAD_NUMERICA, UNIDAD_MEDIDA, PRECIO_UNITARIO 
                    FROM [dbo].[TBL_ESTAMPADO_INSUMO_PROVEEDOR] WHERE ID_PROVEEDOR = @ID", con))
                {
                    cmd.Parameters.AddWithValue("@ID", idProveedor);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            proveedor.ListaInsumos.Add(new E_InsumoProveedor
                            {
                                IdRelacion = GetIntSafe(dr, "ID_RELACION"),
                                IdProveedor = GetIntSafe(dr, "ID_PROVEEDOR"),
                                CodigoArticulo = GetStringSafe(dr, "COD_ARTICULO"),
                                DescripcionPresentacion = GetStringSafe(dr, "DES_PRESENTACION"),
                                CapacidadNumerica = GetDecimalSafe(dr, "CAPACIDAD_NUMERICA"),
                                UnidadMedida = GetStringSafe(dr, "UNIDAD_MEDIDA"),
                                PrecioUnitario = GetDecimalSafe(dr, "PRECIO_UNITARIO")
                            });
                        }
                    }
                }
            }
            return proveedor;
        }

    }
}
