using System;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Web.Configuration;
using RoutingList;

namespace Router
{
    public class Router
    {
        private static HandlerFactory hf = new HandlerFactory();
        private static routerTable cache = new routerTable();
        private static string actionParam;

        public Router() {
            actionParam = WebConfigurationManager.AppSettings["actionParam"];
            actionParam = string.IsNullOrWhiteSpace(actionParam) ? "action" : actionParam.Trim();
        }

        public void doAction(HttpContext context, string url)
        {
            RoutingHandler(context, url);
        }

        private void _doAction(HttpContext context, IHttpHandler handler, RoutingAction routeAction)
        {
            if (routeAction.IsDefAction)
                handler.ProcessRequest(context);
            else
            {
                if (routeAction.action != null)
                    routeAction.action.Invoke(handler, new object[] { context });
                else
                {
                    string msg = "Undefined Action: " + routeAction.actionName;
                    throw new HttpException(404, msg);
                }
            }
            context.Response.End();
        }

        private void RoutingHandler(HttpContext context, string url)
        {
            Routing routing = null;
            RoutingAction routeAction = null;
            DateTime Lastest= Directory.GetLastWriteTime(context.Server.MapPath(url));

            string methodName = context.Request.Params[actionParam];
            methodName = string.IsNullOrWhiteSpace(methodName) ? "" : methodName.Trim();
            string key = "&" + methodName;

            if ((routing = cache.ReadRouting(url)) != null)
            {
                if ((routeAction = cache.ReadAction(url, key)) == null || routeAction.modifyDate != Lastest)
                {
                    if (routing.modifyDate != Lastest)
                    {
                        routing.handler = hf.GetHandler(context, context.Request.RequestType, url, "");
                        routing.modifyDate = Lastest;
                    }

                    routeAction = getRoutingAction(routing.handler, methodName);
                    routeAction.modifyDate = Lastest;
                    if (routeAction.action == null && routeAction.IsDefAction == false)
                        key = "&&";
                    cache.AddOrUpdateAction(url, key, routeAction);
                }
            }
            else
            {
                routing = new Routing();
                routing.handler = hf.GetHandler(context, context.Request.RequestType, url, "");
                routing.modifyDate = Lastest;
                routeAction = getRoutingAction(routing.handler, methodName);
                routeAction.modifyDate = Lastest;
                routing.actions = new Dictionary<string, RoutingAction>();
                if (routeAction.action == null && routeAction.IsDefAction == false)
                    key = "&&";
                routing.actions.Add(key, routeAction);
                cache.AddRouting(url, routing);
            }
            _doAction(context, routing.handler, routeAction);
        }

        private RoutingAction getRoutingAction(IHttpHandler handler, string methodName)
        {
            RoutingAction routeAction = new RoutingAction();
            if (!string.IsNullOrEmpty(methodName))
            {
                routeAction.action = handler.GetType().GetMethod(methodName);
                routeAction.actionName = methodName;
                routeAction.IsDefAction = false;
            }
            else
                routeAction.IsDefAction = true;
            return routeAction;
        }
    }

}
