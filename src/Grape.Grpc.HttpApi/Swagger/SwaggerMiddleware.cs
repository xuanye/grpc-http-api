﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Grape.Grpc.HttpApi.Swagger
{
    public class SwaggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SwaggerOptions _options;
        private readonly ILogger<SwaggerMiddleware> _logger;

        public SwaggerMiddleware(
            SwaggerOptions options,
            RequestDelegate next,
            ILogger<SwaggerMiddleware> logger
            )
        {
            _next = next;
            _options = options ?? new SwaggerOptions();
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext, ISwaggerProvider swaggerProvider)
        {
            if (!RequestingSwaggerDocument(httpContext.Request))
            {
                await _next(httpContext);
                return;
            }

            try
            {
                var swagger = swaggerProvider.GetSwaggerInfo();
                await RespondWithSwaggerJson(httpContext.Response, swagger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                RespondWithNotFound(httpContext.Response);
            }
        }

        private bool RequestingSwaggerDocument(HttpRequest request)
        {
            if (!string.Equals(request.Method, "get", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return _options.RoutePath.Equals(request.Path, StringComparison.CurrentCultureIgnoreCase);
        }

        private void RespondWithNotFound(HttpResponse response)
        {
            response.StatusCode = 404;
        }

        private async Task RespondWithSwaggerJson(HttpResponse response, SwaggerInfo swagger)
        {
            response.StatusCode = 200;
            response.ContentType = "application/json;charset=utf-8";

            var swaggerJson = System.Text.Json.JsonSerializer.Serialize(swagger, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

            await response.WriteAsync(swaggerJson, new UTF8Encoding(false));
        }
    }
}