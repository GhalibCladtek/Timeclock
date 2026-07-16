using System.Web;
using System.Web.Optimization;

namespace TimeClock
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            //bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
            //            "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/adminlte/plugins/jquery.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/adminlte/plugins/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/adminlte/plugins/bootstrap.js"));

            bundles.Add(new ScriptBundle("~/bundles/adminlte").IncludeDirectory(
                      "~/adminlte/dist/js/", "*.min.js", searchSubdirectories: true));

            bundles.Add(new StyleBundle("~/Content/admilteCss").Include(
                      "~/adminlte/dist/adminlte.min.css"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/adminlte/dist/adminlte.min.css",
                      "~/Content/site.css"));
        }
    }
}
