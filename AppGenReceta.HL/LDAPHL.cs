using AppGenReceta.BE;
using System;

using System.Configuration;
using System.DirectoryServices.Protocols;
using System.Globalization;

using System.Net;


namespace AppGenReceta.HL
{
    public static class LDAPHL
    {
        public static EmpleadoBE ValidarUsuario(string Correo, string Clave)
        {
            EmpleadoBE empleadoBE = null;
            
            
            String adminUsername = ConfigurationManager.AppSettings["LDAP_UserName"].ToString();
            String adminPassword = ConfigurationManager.AppSettings["LDAP_Password"].ToString();
            String server = ConfigurationManager.AppSettings["LDAP_Server"].ToString();
            String domain = ConfigurationManager.AppSettings["LDAP_Domain"].ToString();

            try
            {
                LdapConnection ldap = new LdapConnection(new LdapDirectoryIdentifier(server));
                
                
                ldap.AuthType = AuthType.Basic;
                ldap.SessionOptions.ProtocolVersion = 3;
                NetworkCredential nt = new NetworkCredential(adminUsername, adminPassword);
                ldap.Bind(nt);
               
                

                string filter = string.Format(CultureInfo.InvariantCulture, "(&(objectClass=user)(objectCategory=user) (sAMAccountName={0}))", Correo);
                var attributes = new[] { "sAMAccountName", "displayName", "mail" };
                SearchRequest searchRequest = new SearchRequest(domain, filter, SearchScope.Subtree, attributes);

                SearchResponse searchResponse = (SearchResponse)ldap.SendRequest(searchRequest);
                if (1 == searchResponse.Entries.Count)
                {
                    ldap.Bind(new NetworkCredential(Correo, Clave));
                    empleadoBE = new EmpleadoBE();
                    
                    empleadoBE.Correo = searchResponse.Entries[0].Attributes["mail"][0].ToString();
                    empleadoBE.Nombres = searchResponse.Entries[0].Attributes["displayName"][0].ToString();
                }
                else
                {
                    empleadoBE = null;
                    throw new Exception("Login failed.");
                }
            }
            catch (Exception e)
            {

                throw new Exception( e.Message );
            }

            return empleadoBE;
        }
    }
}
