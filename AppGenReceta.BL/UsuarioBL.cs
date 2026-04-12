using AppGenReceta.BE;
using AppGenReceta.DA;
using System;
using System.Collections.Generic;

namespace AppGenReceta.BL
{
    public class UsuarioBL
    {
        public ProveedorBE ValidarUsuario(UsuarioBE user)
        {
            UsuarioDA usuarioDA = new UsuarioDA();
            try
            {
                return usuarioDA.ValidarUsuario(user);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public String ConcatenarCorreos(String area)
        {
            UsuarioDA usuarioDA = new UsuarioDA();
            try
            {
                return usuarioDA.ConcatenarCorreos(area);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}