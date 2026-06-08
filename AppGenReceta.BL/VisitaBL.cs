using AppGenReceta.BE;
using AppGenReceta.DA;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace AppGenReceta.BL
{
    public class VisitaBL
    {
       

        #region SQlServer_Receta
        public List<NPBE> ListarNPS()
        {
            VisitaDA visitaDA = new VisitaDA();
            try
            {
                return visitaDA.ListarNPS();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool ActualizarEstadoAuditoria(int idVisita, string usuario, bool cerrar, string comentario)
        {
            VisitaDA visitaDA = new VisitaDA();
            try
            {
                return visitaDA.ActualizarEstadoAuditoria(idVisita, usuario, cerrar, comentario);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<NPBE> ListarCliente()
        {
            VisitaDA visitaDA = new VisitaDA();
            try
            {
                return visitaDA.ListarCliente();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        public List<NPBE> ListarDatosPorCadaNP()
        {
            VisitaDA visitaDA = new VisitaDA();
            try
            {
                return visitaDA.ListarDatosPorCadaNP();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        public bool RegistrarRecetaAnidada(VisitaBE entidad, string usuario)
        {
            VisitaDA visitaDA = new VisitaDA();
            try
            {
                return visitaDA.RegistrarRecetaAnidada(entidad, usuario);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool EliminarVisita(int id, string comentario, string usuario)
        {
            try
            {
                return new VisitaDA().EliminarVisita(id, comentario, usuario);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public bool ActualizarRecetaCompleta(VisitaBE entidad)
        {
            VisitaDA visitaDA = new VisitaDA();
            try
            {
                return visitaDA.ActualizarRecetaCompleta(entidad);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        

        public List<RecetaInsumoBE> ListarInsumos()
        {
            VisitaDA visitaDA = new VisitaDA();
            try
            {
                return visitaDA.ListarInsumos();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<string> ListarTecnicas(string cliente, string temporada, string estilo, string item)
        {
            VisitaDA visitaDA = new VisitaDA();
            try
            {
                return visitaDA.ListarTecnicas(cliente, temporada, estilo, item);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public VisitaBE ObtenerRecetaPorId(int idVisita)
        {
            return new VisitaDA().ObtenerRecetaPorId(idVisita);
        }

        public List<VisitaBE> ListarRecetasGeneradas(string fechaInicio, string fechaFin)
        {
            return new VisitaDA().ListarRecetasGeneradas(fechaInicio, fechaFin);
        }

        //agregado 11/03/2026
        public List<string> ListarConceptos()
        {
            return new VisitaDA().ListarConceptos();
        }

        // VisitaBL.cs 17/03/2026
        public List<string> ListarConceptosPendientes(string cliente, string temporada, string estilo, string item)
        {
            return new VisitaDA().ListarConceptosPendientes(cliente, temporada, estilo, item);
        }
        
        // VisitaBL.cs 30/03/2026
        public List<string> ListarUbicacionPendientes(string cliente, string temporada, string estilo, string item)
        {
            return new VisitaDA().ListarUbicacionPendientes(cliente, temporada, estilo, item);
        }

        // VisitaBL.cs 17/03/2026
        public List<string> ListarCombosPorEstilo(string cliente, string temporada, string estilo)
        {
            return new VisitaDA().ListarCombosPorEstilo(cliente, temporada, estilo);
        }

        //public List<string> ListarEstilosPorClienteTemporada(string cliente, string temporada)
        //{
        //    return new VisitaDA().ListarEstilosPorClienteTemporada(cliente, temporada);
        //}

        // Inicio 27/03/23
        public List<ItemBE> ListarItems(string cliente, string temporada, string estiloPropio)
        {
            try
            {
                return new VisitaDA().ListarItems(cliente, temporada, estiloPropio);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<EstiloBE> ListarEstilos(string cliente, string temporada)
        {
            try
            {
                return new VisitaDA().ListarEstilos(cliente, temporada);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<EstiloBE> ListarTodosLosEstilos()
        {
            try
            {
                return new VisitaDA().ListarTodosLosEstilos();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public VisitaBE ObtenerDatosEstiloInverso(string estiloBuscar, bool esPropio)
        {
            try
            {
                return new VisitaDA().ObtenerDatosEstiloInverso(estiloBuscar, esPropio);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<EstiloBE> BuscarEstilosInversoAjax(string q, bool esPropio)
        {
            try
            {
                return new VisitaDA().BuscarEstilosInversoAjax(q, esPropio);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        // Fin 27/03/23

        public List<string> ListarItemsPorClienteTemporada(string cliente, string temporada)
        {
            return new VisitaDA().ListarItemsPorClienteTemporada(cliente, temporada);
        }
                    
        public List<string> ListarTemporadaPorCliente(string cliente)
        {
            return new VisitaDA().ListarTemporadaPorCliente(cliente);
        }

        public List<UsuarioBE> ListarUsuariosRoles()
        {
            try
            {
                return new VisitaDA().ListarUsuariosRoles();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public bool ActualizarRol(int idUsuario, int idRol)
        {
            try
            {
                return new VisitaDA().ActualizarRol(idUsuario, idRol);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        // 08/05/2026 - CMendez: Búsqueda por Item
        public ItemDatoCompletoBE BuscarDatosPorItem(string item)
        {
            try
            {
                return new VisitaDA().BuscarDatosPorItem(item);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

    }
}