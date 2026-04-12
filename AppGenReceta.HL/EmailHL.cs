using System;
using System.Configuration;
using System.Net.Mail;
using System.Net;
using System.Net.Security;

namespace AppGenReceta.HL
{
    public static class EmailHL
    {
        public static Boolean Send(String To, String Subject, String Body, String Archivo, String Nombre)
        {
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                  | SecurityProtocolType.Tls11
                                                  | SecurityProtocolType.Tls12;
            }

            Boolean result = false;
            try
            {
                DateTime now = DateTime.UtcNow;
                now = now.AddHours(-5);

                String smtpUserName = "";
                String smtpPassword = "";
                String smtpServer = "";
                int Puerto = 0;

                smtpUserName = ConfigurationManager.AppSettings["Correo"];
                smtpPassword = ConfigurationManager.AppSettings["Clave"];
                smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
                Puerto = int.Parse(ConfigurationManager.AppSettings["Puerto"].ToString());

                String KeycorreoOculto = ConfigurationManager.AppSettings["HabilitarCorreoOculto"];
                String correoOculto = ConfigurationManager.AppSettings["CorreoOculto"];

                char[] delimiterChars = { ';' };
                string[] correosocultos = correoOculto.Split(delimiterChars);

                SmtpClient smtp = new SmtpClient(smtpServer);
                smtp.Port = Puerto;
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = true;

                smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);

                MailMessage mail = new MailMessage();


                //mail.Headers.Add("Message-Id", String.Concat("<", now.ToString("yyMMdd"), ".", now.ToString("HHmmss"), "@libro.com>"));


                mail.From = new MailAddress(smtpUserName, "Gestión de Desarrollo");

                mail.To.Add(new MailAddress(To));
                mail.Subject = Subject;

                if (KeycorreoOculto == "true")
                {
                    for (int i = 0; i < correosocultos.Length; i++)
                    {
                        mail.Bcc.Add(correosocultos[i]);
                    }
                }

                mail.Body = Body;
                mail.IsBodyHtml = true;
                if (!String.IsNullOrEmpty(Archivo))
                {
                    Attachment ArchivoAttachment = new Attachment(Archivo);
                    ArchivoAttachment.Name = Nombre;
                    mail.Attachments.Add(ArchivoAttachment);
                }

                smtp.Send(mail);

                mail.Dispose();
                smtp.Dispose();

                result = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public static Boolean SendVarios(String To, String Subject, String Body, String Archivo, String Nombre)
        {
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                  | SecurityProtocolType.Tls11
                                                  | SecurityProtocolType.Tls12;
            }

            Boolean result = false;
            try
            {
                DateTime now = DateTime.UtcNow;
                now = now.AddHours(-5);

                String smtpUserName = "";
                String smtpPassword = "";
                String smtpServer = "";
                int Puerto = 0;

                smtpUserName = ConfigurationManager.AppSettings["Correo"];
                smtpPassword = ConfigurationManager.AppSettings["Clave"];
                smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
                Puerto = int.Parse(ConfigurationManager.AppSettings["Puerto"].ToString());

                String KeycorreoOculto = ConfigurationManager.AppSettings["HabilitarCorreoOculto"];
                String correoOculto = ConfigurationManager.AppSettings["CorreoOculto"];

                char[] delimiterChars = { ';' };
                string[] correosocultos = correoOculto.Split(delimiterChars);
                string[] to = To.Split(delimiterChars);

                SmtpClient smtp = new SmtpClient(smtpServer);
                smtp.Port = Puerto;
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = true;

                smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);

                MailMessage mail = new MailMessage();


                //mail.Headers.Add("Message-Id", String.Concat("<", now.ToString("yyMMdd"), ".", now.ToString("HHmmss"), "@libro.com>"));


                mail.From = new MailAddress(smtpUserName, "Gestión de Desarrollo");

                //mail.To.Add(new MailAddress(To));
                mail.Subject = Subject;

                if (KeycorreoOculto == "true")
                {
                    for (int i = 0; i < correosocultos.Length; i++)
                    {
                        mail.Bcc.Add(correosocultos[i]);
                    }
                }

                for (int i = 0; i < to.Length; i++)
                {
                    mail.To.Add(to[i]);
                }

                mail.Body = Body;
                mail.IsBodyHtml = true;
                if (!String.IsNullOrEmpty(Archivo))
                {
                    Attachment ArchivoAttachment = new Attachment(Archivo);
                    ArchivoAttachment.Name = Nombre;
                    mail.Attachments.Add(ArchivoAttachment);
                }

                smtp.Send(mail);

                mail.Dispose();
                smtp.Dispose();

                result = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public static Boolean SendCopias(String To, String Subject, String Body, String Archivo, String Nombre, String CorreosCopia)
        {
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                  | SecurityProtocolType.Tls11
                                                  | SecurityProtocolType.Tls12;
            }

            Boolean result = false;
            try
            {
                DateTime now = DateTime.UtcNow;
                now = now.AddHours(-5);

                String smtpUserName = "";
                String smtpPassword = "";
                String smtpServer = "";
                int Puerto = 0;

                smtpUserName = ConfigurationManager.AppSettings["Correo"];
                smtpPassword = ConfigurationManager.AppSettings["Clave"];
                smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
                Puerto = int.Parse(ConfigurationManager.AppSettings["Puerto"].ToString());

                String KeycorreoOculto = ConfigurationManager.AppSettings["HabilitarCorreoOculto"];
                String correoOculto = ConfigurationManager.AppSettings["CorreoOculto"];

                char[] delimiterChars = { ';' };
                string[] correosocultos = correoOculto.Split(delimiterChars);
                string[] correoscopia = CorreosCopia.Split(delimiterChars);

                SmtpClient smtp = new SmtpClient(smtpServer);
                smtp.Port = Puerto;
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = true;

                smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);

                MailMessage mail = new MailMessage();


                //mail.Headers.Add("Message-Id", String.Concat("<", now.ToString("yyMMdd"), ".", now.ToString("HHmmss"), "@libro.com>"));


                mail.From = new MailAddress(smtpUserName, "Gestión de Desarrollo");

                mail.To.Add(new MailAddress(To));
                mail.Subject = Subject;

                if (KeycorreoOculto == "true")
                {
                    for (int i = 0; i < correosocultos.Length; i++)
                    {
                        mail.Bcc.Add(correosocultos[i]);
                    }
                }

                for (int i = 0; i < correoscopia.Length; i++)
                {
                    mail.CC.Add(correoscopia[i]);
                }

                mail.Body = Body;
                mail.IsBodyHtml = true;
                if (!String.IsNullOrEmpty(Archivo))
                {
                    Attachment ArchivoAttachment = new Attachment(Archivo);
                    ArchivoAttachment.Name = Nombre;
                    mail.Attachments.Add(ArchivoAttachment);
                }

                smtp.Send(mail);

                mail.Dispose();
                smtp.Dispose();

                result = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public static Boolean SendMultipleAdjuntoCopias(String To, String Subject, String Body, String[] Archivos, String[] NombreArchivos, String CorreosCopia)
        {
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                  | SecurityProtocolType.Tls11
                                                  | SecurityProtocolType.Tls12;
            }

            Boolean result = false;
            try
            {
                DateTime now = DateTime.UtcNow;
                now = now.AddHours(-5);

                String smtpUserName = "";
                String smtpPassword = "";
                String smtpServer = "";
                int Puerto = 0;

                smtpUserName = ConfigurationManager.AppSettings["Correo"];
                smtpPassword = ConfigurationManager.AppSettings["Clave"];
                smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
                Puerto = int.Parse(ConfigurationManager.AppSettings["Puerto"].ToString());

                String KeycorreoOculto = ConfigurationManager.AppSettings["HabilitarCorreoOculto"];
                String correoOculto = ConfigurationManager.AppSettings["CorreoOculto"];

                char[] delimiterChars = { ';' };
                string[] correosocultos = correoOculto.Split(delimiterChars);
                string[] correoscopia = CorreosCopia.Split(delimiterChars);
                string[] to = To.Split(delimiterChars);

                SmtpClient smtp = new SmtpClient(smtpServer);
                smtp.Port = Puerto;
                smtp.EnableSsl = true;
                smtp.Timeout = 20000;
                //smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = true;

                smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);

                MailMessage mail = new MailMessage();

                mail.From = new MailAddress(smtpUserName, "Sistema de Cobranzas");

                //mail.To.Add(new MailAddress(To));

                mail.Subject = Subject;

                if (KeycorreoOculto == "true")
                {
                    for (int i = 0; i < correosocultos.Length; i++)
                    {
                        mail.Bcc.Add(correosocultos[i]);
                    }
                }

                for (int i = 0; i < to.Length; i++)
                {
                    mail.To.Add(to[i]);
                }

                for (int i = 0; i < correoscopia.Length; i++)
                {
                    mail.CC.Add(correoscopia[i]);
                }

                //Cuando es tipo text se reemplaza para el salto de linea
                mail.Body = Body.Replace("\r\n", "<br>");

                mail.IsBodyHtml = true;

                if (Archivos.Length > 0)
                {
                    for (int i = 0; i < Archivos.Length; i++)
                    {
                        if (!String.IsNullOrEmpty(Archivos[i]))
                        {
                            Attachment ArchivoAttachment = new Attachment(Archivos[i]);
                            ArchivoAttachment.Name = NombreArchivos[i];
                            mail.Attachments.Add(ArchivoAttachment);
                        }
                    }
                }
                smtp.Send(mail);

                mail.Dispose();
                smtp.Dispose();

                result = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }


        //public static Boolean Send(String To, String Subject, String Body, String Archivo, String Nombre)
        //{
        //    {
        //        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
        //                                          | SecurityProtocolType.Tls11
        //                                          | SecurityProtocolType.Tls12;
        //    }

        //    Boolean result = false;
        //    try
        //    {
        //        DateTime now = DateTime.UtcNow;
        //        now = now.AddHours(-5);

        //        String smtpUserName = "";
        //        String smtpPassword = "";
        //        String smtpServer = "";
        //        int Puerto = 0;

        //        smtpUserName = ConfigurationManager.AppSettings["Correo"];
        //        smtpPassword = ConfigurationManager.AppSettings["Clave"];
        //        smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
        //        Puerto = int.Parse(ConfigurationManager.AppSettings["Puerto"].ToString());

        //        String KeycorreoOculto = ConfigurationManager.AppSettings["HabilitarCorreoOculto"];
        //        String correoOculto = ConfigurationManager.AppSettings["CorreoOculto"];

        //        char[] delimiterChars = { ';' };
        //        string[] correosocultos = correoOculto.Split(delimiterChars);

        //        SmtpClient smtp = new SmtpClient(smtpServer);
        //        smtp.Port = Puerto;
        //        smtp.EnableSsl = false;
        //        smtp.UseDefaultCredentials = true;

        //        smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);

        //        MailMessage mail = new MailMessage();


        //        mail.Headers.Add("Message-Id", String.Concat("<", now.ToString("yyMMdd"), ".", now.ToString("HHmmss"), "@libro.com>"));


        //        mail.From = new MailAddress(smtpUserName,"Hoja de Reclamaciones");

        //        mail.To.Add(new MailAddress(To));
        //        mail.Subject = Subject;

        //        if (KeycorreoOculto == "true")
        //        {
        //            for (int i = 0; i < correosocultos.Length; i++)
        //            {
        //                mail.Bcc.Add(correosocultos[i]);
        //            }
        //        }

        //        mail.Body = Body;
        //        mail.IsBodyHtml = true;
        //        if(!String.IsNullOrEmpty(Archivo))
        //        { 
        //            Attachment ArchivoAttachment = new Attachment(Archivo);
        //            ArchivoAttachment.Name = Nombre;
        //            mail.Attachments.Add(ArchivoAttachment);
        //        }

        //        smtp.Send(mail);

        //        mail.Dispose();
        //        smtp.Dispose();

        //        result = true;

        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //    return result;
        //}

        public static Boolean SendTipo1(String To, String Subject, String Body, String Archivo, String Nombre) //Tipo1: Correo que se envia al reclamante/quejoso(a)
        {
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                  | SecurityProtocolType.Tls11
                                                  | SecurityProtocolType.Tls12;
            }

            Boolean result = false;
            try
            {
                DateTime now = DateTime.UtcNow;
                now = now.AddHours(-5);

                String smtpUserName = "";
                String smtpPassword = "";
                String smtpServer = "";
                int Puerto = 0;

                smtpUserName = ConfigurationManager.AppSettings["Correo"];
                smtpPassword = ConfigurationManager.AppSettings["Clave"];
                smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
                Puerto = int.Parse(ConfigurationManager.AppSettings["Puerto"].ToString());

                String KeycorreoOculto = ConfigurationManager.AppSettings["HabilitarCorreoOcultoTipo1"];
                String correoOculto = ConfigurationManager.AppSettings["CorreoOcultoTipo1"];

                char[] delimiterChars = { ';' };
                string[] correosocultos = correoOculto.Split(delimiterChars);

                SmtpClient smtp = new SmtpClient(smtpServer);
                smtp.Port = Puerto;
                smtp.EnableSsl = false;
                smtp.UseDefaultCredentials = true;

                smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);

                MailMessage mail = new MailMessage();


                mail.Headers.Add("Message-Id", String.Concat("<", now.ToString("yyMMdd"), ".", now.ToString("HHmmss"), "@libro.com>"));


                mail.From = new MailAddress(smtpUserName, "Hoja de Reclamaciones");

                mail.To.Add(new MailAddress(To));
                mail.Subject = Subject;

                if (KeycorreoOculto == "true")
                {
                    for (int i = 0; i < correosocultos.Length; i++)
                    {
                        mail.Bcc.Add(correosocultos[i]);
                    }
                }

                mail.Body = Body;
                mail.IsBodyHtml = true;
                if (!String.IsNullOrEmpty(Archivo))
                {
                    Attachment ArchivoAttachment = new Attachment(Archivo);
                    ArchivoAttachment.Name = Nombre;
                    mail.Attachments.Add(ArchivoAttachment);
                }

                smtp.Send(mail);

                mail.Dispose();
                smtp.Dispose();

                result = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public static Boolean SendTipo2(String To, String Subject, String Body, String Archivo, String Nombre) //Tipo2: Correos de alerta al reclamante/quejoso(a)
        {
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                  | SecurityProtocolType.Tls11
                                                  | SecurityProtocolType.Tls12;
            }

            Boolean result = false;
            try
            {
                DateTime now = DateTime.UtcNow;
                now = now.AddHours(-5);

                String smtpUserName = "";
                String smtpPassword = "";
                String smtpServer = "";
                int Puerto = 0;

                smtpUserName = ConfigurationManager.AppSettings["Correo"];
                smtpPassword = ConfigurationManager.AppSettings["Clave"];
                smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
                Puerto = int.Parse(ConfigurationManager.AppSettings["Puerto"].ToString());

                String KeycorreoOculto = ConfigurationManager.AppSettings["HabilitarCorreoOcultoTipo2"];
                String correoOculto = ConfigurationManager.AppSettings["CorreoOcultoTipo2"];

                char[] delimiterChars = { ';' };
                string[] correosocultos = correoOculto.Split(delimiterChars);

                SmtpClient smtp = new SmtpClient(smtpServer);
                smtp.Port = Puerto;
                smtp.EnableSsl = false;
                smtp.UseDefaultCredentials = true;

                smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);

                MailMessage mail = new MailMessage();


                mail.Headers.Add("Message-Id", String.Concat("<", now.ToString("yyMMdd"), ".", now.ToString("HHmmss"), "@libro.com>"));


                mail.From = new MailAddress(smtpUserName, "Hoja de Reclamaciones");

                mail.To.Add(new MailAddress(To));
                mail.Subject = Subject;

                if (KeycorreoOculto == "true")
                {
                    for (int i = 0; i < correosocultos.Length; i++)
                    {
                        mail.Bcc.Add(correosocultos[i]);
                    }
                }

                mail.Body = Body;
                mail.IsBodyHtml = true;
                if (!String.IsNullOrEmpty(Archivo))
                {
                    Attachment ArchivoAttachment = new Attachment(Archivo);
                    ArchivoAttachment.Name = Nombre;
                    mail.Attachments.Add(ArchivoAttachment);
                }

                smtp.Send(mail);

                mail.Dispose();
                smtp.Dispose();

                result = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public static Boolean SendCorreoOculto(String To, String Subject, String Body, String Archivo, String Nombre, Boolean SiCorreoOculto, String CorreoOculto)
        {
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                  | SecurityProtocolType.Tls11
                                                  | SecurityProtocolType.Tls12;
            }

            Boolean result = false;
            try
            {
                DateTime now = DateTime.UtcNow;
                now = now.AddHours(-5);

                String smtpUserName = "";
                String smtpPassword = "";
                String smtpServer = "";
                int Puerto = 0;

                smtpUserName = ConfigurationManager.AppSettings["Correo"];
                smtpPassword = ConfigurationManager.AppSettings["Clave"];
                smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
                Puerto = int.Parse(ConfigurationManager.AppSettings["Puerto"].ToString());

                String KeycorreoOculto;
                if (SiCorreoOculto)
                {
                    KeycorreoOculto = "true";
                }
                else
                {
                    KeycorreoOculto = "false";
                }
                String correoOculto = CorreoOculto;

                char[] delimiterChars = { ';' };
                string[] correosocultos = correoOculto.Split(delimiterChars);

                SmtpClient smtp = new SmtpClient(smtpServer);
                smtp.Port = Puerto;
                smtp.EnableSsl = false;
                smtp.UseDefaultCredentials = true;

                smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);

                MailMessage mail = new MailMessage();


                mail.Headers.Add("Message-Id", String.Concat("<", now.ToString("yyMMdd"), ".", now.ToString("HHmmss"), "@libro.com>"));


                mail.From = new MailAddress(smtpUserName, "Hoja de Reclamaciones");

                mail.To.Add(new MailAddress(To));
                mail.Subject = Subject;

                if (KeycorreoOculto == "true")
                {
                    for (int i = 0; i < correosocultos.Length; i++)
                    {
                        mail.Bcc.Add(correosocultos[i]);
                    }
                }

                mail.Body = Body;
                mail.IsBodyHtml = true;
                if (!String.IsNullOrEmpty(Archivo))
                {
                    Attachment ArchivoAttachment = new Attachment(Archivo);
                    ArchivoAttachment.Name = Nombre;
                    mail.Attachments.Add(ArchivoAttachment);
                }

                smtp.Send(mail);

                mail.Dispose();
                smtp.Dispose();

                result = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public static Boolean SendConCopia(String To, String Subject, String Body, String Archivo, String Nombre, String Correo)
        {
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                  | SecurityProtocolType.Tls11
                                                  | SecurityProtocolType.Tls12;
            }

            Boolean result = false;
            try
            {
                DateTime now = DateTime.UtcNow;
                now = now.AddHours(-5);

                String smtpUserName = "";
                String smtpPassword = "";
                String smtpServer = "";
                int Puerto = 0;

                smtpUserName = ConfigurationManager.AppSettings["Correo"];
                smtpPassword = ConfigurationManager.AppSettings["Clave"];
                smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
                Puerto = int.Parse(ConfigurationManager.AppSettings["Puerto"].ToString());

                String KeycorreoOculto = "false";
                String correoOculto = "";

                char[] delimiterChars = { ';' };
                string[] correosocultos = correoOculto.Split(delimiterChars);

                SmtpClient smtp = new SmtpClient(smtpServer);
                smtp.Port = Puerto;
                smtp.EnableSsl = false;
                smtp.UseDefaultCredentials = true;

                smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);

                MailMessage mail = new MailMessage();


                mail.Headers.Add("Message-Id", String.Concat("<", now.ToString("yyMMdd"), ".", now.ToString("HHmmss"), "@libro.com>"));

                mail.From = new MailAddress(smtpUserName, "Hoja de Reclamaciones");

                mail.To.Add(new MailAddress(To));
                mail.CC.Add(new MailAddress(Correo));
                mail.Subject = Subject;

                if (KeycorreoOculto == "true")
                {
                    for (int i = 0; i < correosocultos.Length; i++)
                    {
                        mail.Bcc.Add(correosocultos[i]);
                    }
                }

                mail.Body = Body;
                mail.IsBodyHtml = true;
                if (!String.IsNullOrEmpty(Archivo))
                {
                    Attachment ArchivoAttachment = new Attachment(Archivo);
                    ArchivoAttachment.Name = Nombre;
                    mail.Attachments.Add(ArchivoAttachment);
                }

                smtp.Send(mail);

                mail.Dispose();
                smtp.Dispose();

                result = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public static Boolean SendMultipleAdjunto(String To, String Subject, String Body, String[] Archivos, String[] NombreArchivos)
        {
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                  | SecurityProtocolType.Tls11
                                                  | SecurityProtocolType.Tls12;
            }

            Boolean result;
            try
            {
                DateTime now = DateTime.UtcNow;
                now = now.AddHours(-5);

                String smtpUserName = "";
                String smtpPassword = "";
                String smtpServer = "";
                int Puerto = 0;

                smtpUserName = ConfigurationManager.AppSettings["Correo"];
                smtpPassword = ConfigurationManager.AppSettings["Clave"];
                smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
                Puerto = int.Parse(ConfigurationManager.AppSettings["Puerto"].ToString());

                String KeycorreoOculto = ConfigurationManager.AppSettings["HabilitarCorreoOculto"];
                String correoOculto = ConfigurationManager.AppSettings["CorreoOculto"];

                char[] delimiterChars = { ';' };
                string[] correosocultos = correoOculto.Split(delimiterChars);

                SmtpClient smtp = new SmtpClient(smtpServer);
                smtp.Port = Puerto;
                smtp.EnableSsl = true;
                smtp.Timeout = 20000;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;

                smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);

                MailMessage mail = new MailMessage();


                mail.Headers.Add("Message-Id", String.Concat("<", now.ToString("yyMMdd"), ".", now.ToString("HHmmss"), "@libro.com>"));


                mail.From = new MailAddress(smtpUserName, "Hoja de Reclamaciones");

                mail.To.Add(new MailAddress(To));
                mail.Subject = Subject;

                if (KeycorreoOculto == "true")
                {
                    for (int i = 0; i < correosocultos.Length; i++)
                    {
                        mail.Bcc.Add(correosocultos[i]);
                    }
                }
                mail.Body = Body;
                mail.IsBodyHtml = true;


                if (Archivos.Length > 0)
                {
                    for (int i = 0; i < Archivos.Length; i++)
                    {
                        Attachment ArchivoAttachment = new Attachment(Archivos[i]);
                        ArchivoAttachment.Name = NombreArchivos[i];
                        mail.Attachments.Add(ArchivoAttachment);
                    }
                }

                smtp.Send(mail);

                mail.Dispose();
                smtp.Dispose();

                result = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public static Boolean SendMultipleAdjuntoCorreoOculto(String To, String Subject, String Body, String[] Archivos, String[] NombreArchivos, Boolean SiCorreoOculto, String CorreoOculto)
        {
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                  | SecurityProtocolType.Tls11
                                                  | SecurityProtocolType.Tls12;
            }

            Boolean result = false;
            try
            {
                DateTime now = DateTime.UtcNow;
                now = now.AddHours(-5);

                String smtpUserName = "";
                String smtpPassword = "";
                String smtpServer = "";
                int Puerto = 0;

                smtpUserName = ConfigurationManager.AppSettings["Correo"];
                smtpPassword = ConfigurationManager.AppSettings["Clave"];
                smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
                Puerto = int.Parse(ConfigurationManager.AppSettings["Puerto"].ToString());

                String KeycorreoOculto;
                if (SiCorreoOculto)
                {
                    KeycorreoOculto = "true";
                }
                else
                {
                    KeycorreoOculto = "false";
                }
                String correoOculto = CorreoOculto;

                char[] delimiterChars = { ';' };
                string[] correosocultos = correoOculto.Split(delimiterChars);

                SmtpClient smtp = new SmtpClient(smtpServer);
                smtp.Port = Puerto;
                smtp.EnableSsl = true;
                smtp.Timeout = 20000;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;

                smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);

                MailMessage mail = new MailMessage();


                mail.Headers.Add("Message-Id", String.Concat("<", now.ToString("yyMMdd"), ".", now.ToString("HHmmss"), "@libro.com>"));


                mail.From = new MailAddress(smtpUserName, "Hoja de Reclamaciones");

                mail.To.Add(new MailAddress(To));
                mail.Subject = Subject;

                if (KeycorreoOculto == "true")
                {
                    for (int i = 0; i < correosocultos.Length; i++)
                    {
                        mail.Bcc.Add(correosocultos[i]);
                    }
                }
                mail.Body = Body;
                mail.IsBodyHtml = true;


                if (Archivos.Length > 0)
                {
                    for (int i = 0; i < Archivos.Length; i++)
                    {
                        Attachment ArchivoAttachment = new Attachment(Archivos[i]);
                        ArchivoAttachment.Name = NombreArchivos[i];
                        mail.Attachments.Add(ArchivoAttachment);
                    }
                }

                smtp.Send(mail);

                mail.Dispose();
                smtp.Dispose();

                result = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }
    }
}
