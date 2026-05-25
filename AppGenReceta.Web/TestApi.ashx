<%@ WebHandler Language="C#" Class="TestHandler" %>
using System;
using System.Web;
using AppGenReceta.DA;

public class TestHandler : IHttpHandler {
    public void ProcessRequest(HttpContext context) {
        context.Response.ContentType = "text/plain";
        try {
            var da = new Liquidacion_DA();
            var res = da.ObtenerNPsPendientes();
            context.Response.Write("Count: " + res.Count + "\n");
            foreach(var item in res) {
                context.Response.Write(item.NP + " - " + item.Cliente + "\n");
            }
        } catch (Exception ex) {
            context.Response.Write("ERROR: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
    public bool IsReusable { get { return false; } }
}
