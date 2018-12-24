using System;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Configuration;
using System.Web.Script.Serialization;
using Router.RoutingList;
using Router.Attributes;

namespace Router
{
    internal static class Router
    {
        private static HandlerFactory hf = new HandlerFactory();
        private static routerTable cache = new routerTable();
        private static JavaScriptSerializer j = new JavaScriptSerializer();
        private static string actionParam 
        {
            get{
                string act=WebConfigurationManager.AppSettings["actionParam"];
                return string.IsNullOrWhiteSpace(act) ? "action" : act.Trim();
            }
        }

        public static void doAction(HttpContext context, string url)
        {
            RoutingHandler(context, url);
        }

        private static void _doAction(HttpContext context, IHttpHandler handler, RoutingAction routeAction)
        {
            if (routeAction.IsDefAction)
                handler.ProcessRequest(context);
            else
                _invoke(context, handler, routeAction);
            context.Response.End();
        }

        private static void _invoke(HttpContext context, IHttpHandler handler, RoutingAction routeAction)
        {
            if (routeAction.attribute == null)
                routeAction.action.Invoke(handler, new object[] { context });
            else
            {
                object[] obj = new object[routeAction.param.Length];
                NameValueCollection urlParams = new NameValueCollection();

                if (routeAction.attribute.UseHttpGet)
                    urlParams = context.Request.QueryString;
                else
                    urlParams = context.Request.Form;

                int i = 0;
                foreach (var p in routeAction.param)
                {
                    string urlParam = urlParams.Get(p.Name);
                    Type type;
                    if (p.IsOut || p.ParameterType.IsByRef)
                        type = p.ParameterType.Assembly.GetType(p.ParameterType.FullName.TrimEnd('&'));
                    else
                        type = p.ParameterType;

                    if (urlParam == null)
                    {
                        string msg = string.Format("参数不能为空", p.Name);
                        throw new ArgumentException(msg, p.Name);
                    }

                    if (type.Equals(urlParam.GetType()))
                        obj.SetValue(urlParam, i++);
                    else
                    {
                        try
                        {
                            if (type.IsPrimitive)
                                obj.SetValue(Convert.ChangeType(urlParam, type), i++);
                            else
                                obj.SetValue(j.ConvertToType(j.Deserialize<object>(urlParam), type), i++);
                        }
                        catch (Exception e)
                        {
                            string msg = string.Format("无法将 {0} 转换为 {1}", urlParam, type.FullName);
                            throw new ArgumentException(msg, p.Name);
                        }
                    }
                }

                object result = routeAction.action.Invoke(handler, obj);
                if (routeAction.attribute.JSONSerializeString)
                    context.Response.ContentType = "text/json";
                else
                    context.Response.ContentType = "text/plain";
                
                if (result.GetType().Name == "String")
                {
                    try
                    {
                        result = j.Deserialize<object>((string)result);
                    }
                    catch (Exception e){}
                }
                context.Response.Write(j.Serialize(new { result }));
            }
        }

        private static void RoutingHandler(HttpContext context, string url)
        {
            Routing routing = null;
            RoutingAction routeAction = null;
            DateTime Lastest = Directory.GetLastWriteTime(context.Server.MapPath(url));

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
                        if (routing.handler.IsReusable)
                            routing.modifyDate = Lastest;
                    }

                    routeAction = getRoutingAction(routing.handler, methodName, context);
                    routeAction.modifyDate = Lastest;
                    if (routing.handler.IsReusable)
                        cache.AddOrUpdateAction(url, key, routeAction);
                }
            }
            else
            {
                routing = new Routing();
                routing.handler = hf.GetHandler(context, context.Request.RequestType, url, "");
                routing.modifyDate = Lastest;
                routeAction = getRoutingAction(routing.handler, methodName, context);
                routeAction.modifyDate = Lastest;
                routing.actions = new Dictionary<string, RoutingAction>();
                routing.actions.Add(key, routeAction);
                if (routing.handler.IsReusable)
                    cache.AddRouting(url, routing);
            }
            _doAction(context, routing.handler, routeAction);
        }

        private static RoutingAction getRoutingAction(IHttpHandler handler, string methodName, HttpContext context)
        {
            RoutingAction routeAction = new RoutingAction();
            if (!string.IsNullOrWhiteSpace(methodName))
            {
                routeAction.action = handler.GetType().GetMethod(methodName);

                if (routeAction.action != null)
                {
                    routeAction.actionName = methodName;
                    routeAction.IsDefAction = false;

                    object[] attr = routeAction.action.GetCustomAttributes(new RoutingMethodAttribute().GetType(), false);
                    var param = routeAction.action.GetParameters();
                    if (attr.Length > 0)
                    {
                        routeAction.param = param;
                        routeAction.attribute = (RoutingMethodAttribute)attr[0];
                        routeAction.returnType = routeAction.action.ReturnParameter;
                    }
                    else
                        if (param.Length != 1 || (param.Length == 1 && !param[0].ParameterType.Equals(context.GetType())))
                            throw new HttpException(500, "访问未添加RoutingMethod特性的方法时，请确认方法只有一个HttpContext类型参数，当方法有多个参数时请对方法标记RoutingMethod特性。");
                }
                else
                {
                    string msg = string.Format("方法'{0}'不可访问，请检查方法是否定义或受保护级别限制。", methodName);
                    throw new HttpException(500, msg, new HttpUnhandledException().InnerException);
                }
            }
            else
                routeAction.IsDefAction = true;
            return routeAction;
        }
    }
}
