using System.Web;
using System.Web.Mvc;

namespace TimeClock
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // Add your custom filter globally
            filters.Add(new CustomSessionAuthorizeAttribute());
        }
    }
}
