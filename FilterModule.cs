using System;
using System.Web;
using System.Web.SessionState;

namespace Router
{
    /// <summary>
    /// author:xyz
    /// date:2018-12-07
    /// </summary>
    internal class FilterModule : IHttpModule
    {
        public void Dispose() { }

        public void Init(HttpApplication context)
        {
            context.PostResolveRequestCache += new EventHandler(this.onPostResolveRequestCache);
            context.AcquireRequestState += RouterFilter.Filter._AcquireRequestState;
        }

        private void onPostResolveRequestCache(Object sender, EventArgs s)
        {
            HttpApplication app = (HttpApplication)sender;
            HttpContext context = app.Context;
            string Ext = context.Request.CurrentExecutionFilePathExtension;
            string[] path_section = context.Request.Path.Split('/');

            if (path_section[path_section.Length - 1].IndexOf('.') > 0 && Ext.ToUpper() == ".ROUTER")
            {
                context.RemapHandler(new ForwardHandler());
            }
            else
                context.RemapHandler(context.CurrentHandler);
        }
    }

    internal sealed class ForwardHandler : IHttpHandler,IReadOnlySessionState{

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            string[] path_section = context.Request.Path.Split('/');
            string[] file_section = path_section[path_section.Length - 1].Split('.');
            file_section.SetValue(file_section[file_section.Length - 1].ToUpper().Replace("ROUTER", "ashx"), file_section.Length - 1);
            string file = string.Join(".", file_section);
            path_section.SetValue(file, path_section.Length - 1);
            string realPath = string.Join("/", path_section);
            Router.doAction(context, realPath);
        }
    }
}

