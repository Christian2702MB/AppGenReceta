using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace AppGenReceta.Web
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                ////Convivir en paralelo aplicacion actual
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
                //defaults: new { controller = "Account", action = "Index", id = UrlParameter.Optional } 
                ////Ir directo app de responsable logistico
                //defaults: new { controller = "Home", action = "IndexRespLogistico", id = UrlParameter.Optional } 
                ////Ir directo app de responsable logistico
                //defaults: new { controller = "Home", action = "Responsable", id = UrlParameter.Optional } 
            );
        }
    }
}
