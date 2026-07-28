using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace TimeClock
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_EndRequest()
        {
            var context = new HttpContextWrapper(Context);

            // If the system is trying to redirect to login (302), BUT it's an AJAX request...
            if (context.Response.StatusCode == 302 && context.Request.IsAjaxRequest())
            {
                // Cancel the redirect and force a 401 Unauthorized status
                context.Response.Clear();
                context.Response.StatusCode = 401;
            }
        }
    }
}