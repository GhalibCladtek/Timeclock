using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

public class CustomSessionAuthorizeAttribute : AuthorizeAttribute
{
    // 1. Allow [AllowAnonymous] to bypass this filter (for Login page)
    public override void OnAuthorization(AuthorizationContext filterContext)
    {
        bool skipAuthorization = filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                              || filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);

        if (skipAuthorization)
        {
            return;
        }

        base.OnAuthorization(filterContext);
    }

    // 2. The core authorization and "Auto-Login" logic
    protected override bool AuthorizeCore(HttpContextBase httpContext)
    {
        // SCENARIO A: The user has an active server session. Let them through.
        if (httpContext.Session != null && httpContext.Session["Username"] != null)
        {
            return true;
        }

        // SCENARIO B: The session is dead. Let's check for the 30-day "Remember Me" cookie.
        HttpCookie authCookie = httpContext.Request.Cookies["AuthCookie"];

        if (authCookie != null && !string.IsNullOrEmpty(authCookie.Value))
        {
            // DECRYPT THE COOKIE TO GET THE REAL USERNAME!
            string storedUsername = TimeClock.Helpers.CookieSecurity.Decrypt(authCookie.Value);

            // If decryption failed (tampering detected), storedUsername will be null
            if (!string.IsNullOrEmpty(storedUsername))
            {
                try
                {
                    // Connect to your database to get the user's fresh details
                    using (var db = new TimeClock.Models.TaskLogEntities())
                    {
                        var data = db.TblUsers.FirstOrDefault(u => u.username == storedUsername);

                        if (data != null)
                        {
                            // THE MAGIC: Rebuild the session in the background
                            httpContext.Session["Id"] = data.Id;
                            httpContext.Session["Username"] = data.username;
                            httpContext.Session["BadgeId"] = data.badgeId;
                            httpContext.Session["FullName"] = data.fullName;
                            httpContext.Session["Title"] = data.title;
                            httpContext.Session["LevelPos"] = data.levelPosition;

                            // Authorization passes! The user doesn't even know their session had died.
                            return true;
                        }
                    }
                }
                catch
                {
                    // If the DB fails, swallow the error and force them to log in normally
                    return false;
                }
            }
        }

        // SCENARIO C: No active session AND no 30-day cookie. Deny access.
        return false;
    }

    // 3. What to do when unauthorized (Redirect to Login)
    protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
    {
        // Check if it's an AJAX request. If so, return a 401 status code instead of a redirect
        if (filterContext.HttpContext.Request.IsAjaxRequest())
        {
            //filterContext.HttpContext.Response.StatusCode = 401;
            //filterContext.HttpContext.Response.End();
            filterContext.Result = new HttpStatusCodeResult(401, "Session Expired");
        }
        else
        {
            // Standard web request: redirect to Login Controller, Index Action
            filterContext.Result = new RedirectToRouteResult(
                new System.Web.Routing.RouteValueDictionary(
                    new { controller = "Login", action = "Index" }
                )
            );
        }
    }
}