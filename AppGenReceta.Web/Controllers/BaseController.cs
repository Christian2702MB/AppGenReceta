using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AppGenReceta.Web.Controllers
{
    public class BaseController : Controller
    {
        // GET: Base
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            String ExecutionPath = filterContext.HttpContext.Request.CurrentExecutionFilePath;

            if (filterContext.HttpContext.Session["EmpleadoAdmin"] == null)
            {
                if (filterContext.HttpContext.Request.IsAjaxRequest())
                {
                    filterContext.HttpContext.Response.StatusCode = 401;
                    filterContext.Result = new JsonResult { Data = "Unauthorized", JsonRequestBehavior = JsonRequestBehavior.AllowGet };
                }
                else
                {
                    filterContext.Result = RedirectToAction("Index", "Home");
                }
            }
        }
    }
}