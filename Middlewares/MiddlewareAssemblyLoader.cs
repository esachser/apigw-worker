using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using BaseMiddleware;

namespace WorkerApi
{
    class MiddlewareAssemblyLoader
    {
        private readonly ILogger<MiddlewareAssemblyLoader> _logger;
        private readonly Dictionary<string, Type> _middlewares;
        private readonly IMemoryCache _cache;
        public MiddlewareAssemblyLoader(ILogger<MiddlewareAssemblyLoader> logger, IMemoryCache memmoryCache)
        {
            _logger = logger;

            var assembly =
                Assembly.LoadFrom("../DevMiddleware/bin/Debug/netcoreapp3.1/DevMiddleware.dll");
            
            _middlewares = new Dictionary<string, Type>();

            var middlewareClasses = assembly.GetTypes().Where(t => t.GetInterface(typeof(IBaseMiddleware).Name) != null);

            foreach (var mid in middlewareClasses)
            {
                _logger.LogInformation("Type: " + mid.FullName);
                _middlewares[mid.FullName] = mid;
            }

            _cache = memmoryCache;
        }

        public IBaseMiddleware LoadMiddleware(string middleware, params object[] parms)
        {
            var key = middleware;
            var item = _middlewares[middleware];
            var middlewareObj = Activator.CreateInstance(item, parms) as IBaseMiddleware;
            return middlewareObj;
        }
    }
}