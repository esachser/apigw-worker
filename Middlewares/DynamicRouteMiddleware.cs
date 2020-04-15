using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BaseMiddleware;

namespace WorkerApi
{
    class DynamicRouteMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DynamicRouteMiddleware> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly MiddlewareAssemblyLoader _assemblyLoader;

        public DynamicRouteMiddleware(
            RequestDelegate next,
            ILogger<DynamicRouteMiddleware> logger,
            IHttpClientFactory clientFactory,
            MiddlewareAssemblyLoader assemblyLoader)
        {
            _next = next;
            _logger = logger;
            _clientFactory = clientFactory;
            _assemblyLoader = assemblyLoader;
        }

        static RouteTemplate template = TemplateParser.Parse("/teste/{id}");
        static TemplateMatcher matcher = new TemplateMatcher(template, new RouteValueDictionary());

        public async Task Invoke(HttpContext ctx)
        {
            ctx.Response.StatusCode = 404;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            RouteValueDictionary values = new RouteValueDictionary();

            if (matcher.TryMatch(ctx.Request.Path, values))
            {
                _logger.LogInformation("Deu match: " + values["id"]);
                _logger.LogInformation($"Tempo para match: {stopwatch.ElapsedMilliseconds}ms");
                stopwatch.Restart();

                var endpointFeature = ctx.Features[typeof(IEndpointFeature)] as IEndpointFeature;
                endpointFeature.Endpoint = new RouteEndpoint(_next, template.ToRoutePattern(), 1, null, endpointFeature.Endpoint.DisplayName);

                ctx.Request.RouteValues = values;

                _logger.LogInformation($"Tempo para configuração da rota: {stopwatch.ElapsedMilliseconds}ms");
                stopwatch.Restart();
                var devMid = _assemblyLoader.LoadMiddleware("DevMiddleware.DevMiddleware", values["id"].ToString());
                _logger.LogInformation($"Tempo de carga do Middleware: {stopwatch.ElapsedMilliseconds}ms");
                stopwatch.Restart();
                // devMidType.GetMethod("Invoke").Invoke(devMid, new object[] {ctx, null});
                await devMid.Invoke(ctx, null);
                _logger.LogInformation($"Tempo de execução do middleware: {stopwatch.ElapsedMilliseconds}ms");
                stopwatch.Restart();
            }
            else
            {
                await ctx.Response.WriteAsync("Not found");
            }
            stopwatch.Stop();
        }
    }
}