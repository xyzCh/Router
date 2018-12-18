using System;
using System.Web;
using System.Web.Compilation;

namespace Router
{
    internal class HandlerFactory : IHttpHandlerFactory
    {
        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            Type handlerType = BuildManager.GetCompiledType(url);
            IHttpHandler handler = (IHttpHandler)Activator.CreateInstance(handlerType, true);
            return handler;
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
        }
    }
}
