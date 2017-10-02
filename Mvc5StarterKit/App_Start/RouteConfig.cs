﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Mvc5StarterKit
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            var izendaApiPrefix = System.Configuration.ConfigurationManager.AppSettings["izendaapiprefix"];

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapMvcAttributeRoutes();

            routes.MapRoute(
                name: "ReportPart",
                url: "viewer/reportpart/{id}",
                defaults: new { controller = "Home", action = "ReportPart" }
            );

            routes.MapRoute(
                name: "ReportViewer",
                url: "report/view/{id}",
                defaults: new { controller = "Report", action = "ReportViewer" }
            );

            routes.MapRoute(
                name: "DashboardViewer",
                url: "dashboard/edit/{id}",
                defaults: new { controller = "Dashboard", action = "DashboardViewer" }
            );

            routes.MapRoute(
               name: "CustomAuth",
               url: $"{izendaApiPrefix}user/login",
               defaults: new { controller = "Home", action = "CustomAuth" }
           );

            routes.IgnoreRoute($"{izendaApiPrefix}/{{*pathInfo}}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
