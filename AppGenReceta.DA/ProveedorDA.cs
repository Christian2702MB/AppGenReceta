using AppGenReceta.BE;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace AppGenReceta.DA
{
    public class ProveedorDA
    {
        private String ConnectionString = ConfigurationManager.ConnectionStrings["AppGenReceta_SQL"].ConnectionString;
        private String ConnectionStringPrueba = ConfigurationManager.ConnectionStrings["AppGenReceta_SQL_Prueba"].ConnectionString;

        #region SharepointCAMLQuery

        static string urlPlatLibroRec = ConfigurationManager.AppSettings["URLSitioWeb"];
        static string usr = ConfigurationManager.AppSettings["UsuarioSitioWeb"];
        static string pwd = ConfigurationManager.AppSettings["ClaveSitioWeb"];

        public string EncriptarBase64(string cadenaEncriptar)
        {
            var cadenaEncriptarBytes = System.Text.Encoding.
                UTF8.GetBytes(cadenaEncriptar);
            return System.Convert.ToBase64String(cadenaEncriptarBytes);
        }

        #endregion

    }

}
