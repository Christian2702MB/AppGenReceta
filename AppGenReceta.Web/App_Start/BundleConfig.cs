using System.Web.Optimization;

namespace AppGenReceta.Web
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            // ── Scripts base (jQuery + Bootstrap) ──────────────────────────
            bundles.Add(new ScriptBundle("~/bundles/base").Include(
                "~/Scripts/jquery.min.js",
                "~/Scripts/bootstrap.min.js"
            ));

            // ── Scripts por módulo (carga sólo en las vistas que los necesitan)
            bundles.Add(new ScriptBundle("~/bundles/visita").Include(
                "~/Scripts/Home/Visita.js"
            ));

            bundles.Add(new ScriptBundle("~/bundles/visitaSctr").Include(
                "~/Scripts/Home/VisitaCorregirSCTR.js"
            ));

            bundles.Add(new ScriptBundle("~/bundles/index").Include(
                "~/Scripts/Home/Index.js"
            ));

            bundles.Add(new ScriptBundle("~/bundles/mantenimiento").Include(
                "~/Scripts/Home/Mantenimiento.js"
            ));

            // ── Estilos ────────────────────────────────────────────────────
            bundles.Add(new StyleBundle("~/bundles/css").Include(
                "~/Content/bootstrap.min.css"
            ));

            // Habilitar optimización (minificación + concatenación)
            // en producción (debug=false) se activa automáticamente;
            // descomentar la siguiente línea sólo para pruebas locales:
            // BundleTable.EnableOptimizations = true;
        }
    }
}
