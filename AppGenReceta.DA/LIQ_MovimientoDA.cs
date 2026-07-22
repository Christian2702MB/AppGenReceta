using AppGenReceta.BE;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AppGenReceta.DA
{
    public class LIQ_MovimientoDA
    {
        private string ConnectionString = ConfigurationManager.ConnectionStrings["AppGenReceta_SQL"].ConnectionString;

        public List<LIQ_MOV_SolicitudBE> ListarSolicitudes(string usuarioActual, string rolUsuario)
        {
            var lista = new List<LIQ_MOV_SolicitudBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("LIQ_MOV_SP_ListarSolicitudes", cnx))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@UsuarioActual", usuarioActual);
                        cmd.Parameters.AddWithValue("@RolUsuario", rolUsuario);

                        cnx.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                lista.Add(new LIQ_MOV_SolicitudBE
                                {
                                    IdSolicitud = Convert.ToInt32(dr["IdSolicitud"]),
                                    TipoMovimiento = dr["TipoMovimiento"].ToString(),
                                    TipoAprobador = dr["TipoAprobador"].ToString(),
                                    Estado = dr["Estado"].ToString(),
                                    FechaSolicitud = Convert.ToDateTime(dr["FechaSolicitud"]),
                                    UsuarioLiquidador = dr["UsuarioLiquidador"].ToString(),
                                    UsuarioAprobador = dr["UsuarioAprobador"] != DBNull.Value ? dr["UsuarioAprobador"].ToString() : "",
                                    NumMovimientoERP = dr["NumMovimientoERP"] != DBNull.Value ? dr["NumMovimientoERP"].ToString() : "",
                                    Almacen = dr["Almacen"].ToString(),
                                    Observaciones = dr["Observaciones"] != DBNull.Value ? dr["Observaciones"].ToString() : ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lista;
        }

        public int RegistrarSolicitud(LIQ_MOV_SolicitudBE solicitud)
        {
            int idSolicitud = 0;
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                cnx.Open();
                using (SqlTransaction tr = cnx.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmdCab = new SqlCommand("LIQ_MOV_SP_RegistrarSolicitud", cnx, tr))
                        {
                            cmdCab.CommandType = CommandType.StoredProcedure;
                            cmdCab.Parameters.AddWithValue("@TipoMovimiento", solicitud.TipoMovimiento);
                            cmdCab.Parameters.AddWithValue("@TipoAprobador", solicitud.TipoAprobador);
                            cmdCab.Parameters.AddWithValue("@UsuarioLiquidador", solicitud.UsuarioLiquidador);
                            cmdCab.Parameters.AddWithValue("@Almacen", solicitud.Almacen);
                            cmdCab.Parameters.AddWithValue("@Observaciones", solicitud.Observaciones ?? "");
                            cmdCab.Parameters.AddWithValue("@NroGuia", solicitud.NroGuia ?? "");

                            SqlParameter outId = new SqlParameter("@IdSolicitud", SqlDbType.Int);
                            outId.Direction = ParameterDirection.Output;
                            cmdCab.Parameters.Add(outId);

                            cmdCab.ExecuteNonQuery();
                            idSolicitud = Convert.ToInt32(outId.Value);
                        }

                        foreach (var det in solicitud.Detalles)
                        {
                            using (SqlCommand cmdDet = new SqlCommand("LIQ_MOV_SP_RegistrarSolicitudDetalle", cnx, tr))
                            {
                                cmdDet.CommandType = CommandType.StoredProcedure;
                                cmdDet.Parameters.AddWithValue("@IdSolicitud", idSolicitud);
                                cmdDet.Parameters.AddWithValue("@NP", det.NP ?? "");
                                cmdDet.Parameters.AddWithValue("@CodItem", det.CodItem);
                                cmdDet.Parameters.AddWithValue("@NombreItem", det.NombreItem ?? "");
                                cmdDet.Parameters.AddWithValue("@UM", det.UM ?? "");
                                cmdDet.Parameters.AddWithValue("@Cantidad", det.Cantidad);
                                cmdDet.Parameters.AddWithValue("@CodMermaOrigen", det.CodMermaOrigen ?? "");
                                cmdDet.Parameters.AddWithValue("@Lote", det.Lote ?? "");
                                cmdDet.Parameters.AddWithValue("@LoteProv", det.LoteProv ?? "");
                                cmdDet.ExecuteNonQuery();
                            }
                        }

                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
            return idSolicitud;
        }

        public void AprobarSolicitud(int idSolicitud, string usuarioAprobador, out string numMovimientoERP)
        {
            numMovimientoERP = "";
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("LIQ_MOV_SP_AprobarSolicitud", cnx))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdSolicitud", idSolicitud);
                    cmd.Parameters.AddWithValue("@UsuarioAprobador", usuarioAprobador);

                    SqlParameter outNumERP = new SqlParameter("@NumMovimientoERP", SqlDbType.VarChar, 20);
                    outNumERP.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(outNumERP);

                    cnx.Open();
                    cmd.ExecuteNonQuery();

                    numMovimientoERP = outNumERP.Value.ToString();
                }
            }
        }

        public void ObservarSolicitud(int idSolicitud, string usuarioAprobador, string observacion)
        {
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("LIQ_MOV_SP_ObservarSolicitud", cnx))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdSolicitud", idSolicitud);
                    cmd.Parameters.AddWithValue("@UsuarioAprobador", usuarioAprobador);
                    cmd.Parameters.AddWithValue("@Observacion", observacion);

                    cnx.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<LIQ_MOV_SolicitudDetalleBE> ObtenerDetalles(int idSolicitud)
        {
            var lista = new List<LIQ_MOV_SolicitudDetalleBE>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("LIQ_MOV_SP_ObtenerDetalleSolicitud", cnx))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdSolicitud", idSolicitud);

                    cnx.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_MOV_SolicitudDetalleBE
                            {
                                IdDetalle = Convert.ToInt32(dr["IdDetalle"]),
                                IdSolicitud = Convert.ToInt32(dr["IdSolicitud"]),
                                NP = dr["NP"] != DBNull.Value ? dr["NP"].ToString() : "",
                                CodItem = dr["CodItem"].ToString(),
                                NombreItem = dr["NombreItem"] != DBNull.Value ? dr["NombreItem"].ToString() : "",
                                UM = dr["UM"] != DBNull.Value ? dr["UM"].ToString() : "",
                                Cantidad = Convert.ToDecimal(dr["Cantidad"]),
                                CodMermaOrigen = dr["CodMermaOrigen"] != DBNull.Value ? dr["CodMermaOrigen"].ToString() : "",
                                Lote = dr["Lote"] != DBNull.Value ? dr["Lote"].ToString() : "",
                                LoteProv = dr["LoteProv"] != DBNull.Value ? dr["LoteProv"].ToString() : ""
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public void EjecutarMovimientoSTK(string observaciones, DateTime fechaMovimiento, string usuario, List<string> nps)
        {
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                // Concatenar NPs al inicio de las observaciones
                string obsFinal = observaciones ?? "";
                if (nps != null && nps.Count > 0)
                {
                    string npString = string.Join(", ", nps);
                    obsFinal = string.IsNullOrWhiteSpace(obsFinal) ? npString : npString + " - " + obsFinal;
                }

                // Truncar si es muy largo para evitar errores SQL (ej. max 200 chars)
                if (obsFinal.Length > 200) obsFinal = obsFinal.Substring(0, 200);

                string sql = @"
                    EXEC SP_LG_MOVISTK  'I', '60', '', '188', '', '', '', '', '60', @usuario, '', @observaciones, @fechaMovimiento, '' ,'N','','0', 0,'','',''
                ";
                using (SqlCommand cmd = new SqlCommand(sql, cnx))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@observaciones", obsFinal);
                    cmd.Parameters.AddWithValue("@fechaMovimiento", fechaMovimiento.ToString("dd/MM/yyyy"));
                    
                    // Asegurar que el usuario no sobrepase la longitud del parámetro en la BD (ej. 10 chars)
                    string userClean = string.IsNullOrWhiteSpace(usuario) ? "USER" : usuario;
                    if (userClean.Length > 10) userClean = userClean.Substring(0, 10);
                    
                    cmd.Parameters.AddWithValue("@usuario", userClean);

                    cnx.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
