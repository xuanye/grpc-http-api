using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.HttpApi.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grpc.HttpApi.Internal
{
    internal class UnaryServerCallHandler<TService, TRequest, TResponse>
       where TService : class
       where TRequest : class
       where TResponse : class
    {
        private readonly UnaryServerMethodInvoker<TService, TRequest, TResponse> _unaryMethodInvoker;
        private readonly JsonFormatter _jsonFormatter;
        private readonly IJsonParser _jsonParser;
        private readonly ConcurrentDictionary<string, List<FieldDescriptor>> _pathDescriptorsCache;

        private static readonly List<string> AllowContentTypes = new List<string>() {
            "application/x-www-form-urlencoded", "multipart/form-data","application/json" };

        private readonly HttpApiOption _httpApiOption;

        public UnaryServerCallHandler(
            UnaryServerMethodInvoker<TService, TRequest, TResponse> unaryMethodInvoker,
            HttpApiOption httpApiOption,
            GrpcHttpApiOptions grpcHttpApiOptions)
        {
            _unaryMethodInvoker = unaryMethodInvoker;
            _jsonFormatter = grpcHttpApiOptions.JsonFormatter;
            _jsonParser = grpcHttpApiOptions.JsonParser;

            _pathDescriptorsCache = new ConcurrentDictionary<string, List<FieldDescriptor>>(StringComparer.Ordinal);
            _httpApiOption = httpApiOption;
        }

        public async Task HandleCallAsync(HttpContext httpContext)
        {
            var serviceProvider = httpContext.RequestServices;

            IHttpApiOutputProcess outputProcess = serviceProvider.GetService<IHttpApiOutputProcess>();
            IHttpApiErrorProcess errorProcess = serviceProvider.GetService<IHttpApiErrorProcess>();

            IHttpPlugin plugin = null;

            if (_httpApiOption.PluginType != null)
            {
                plugin = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, _httpApiOption.PluginType) as IHttpPlugin;
            }
            var selectedEncoding = ResponseEncoding.SelectCharacterEncoding(httpContext.Request);

            (IMessage, StatusCode, string) tuple;
            if (plugin != null && plugin is IHttpRequestParsePlugin parsePlugin)
            {
                //replace request message parse
                tuple = await parsePlugin.ParseAsync(httpContext.Request);
            }
            else
            {
                tuple = await CreateMessage(httpContext.Request);
            }

            var (requestMessage, requestStatusCode, errorMessage) = tuple;

            if (requestMessage == null || requestStatusCode != StatusCode.OK)
            {
                await SendErrorResponse(errorProcess, httpContext.Response, selectedEncoding, errorMessage ?? string.Empty, requestStatusCode);
                return;
            }

            //post parse
            if (plugin != null && plugin is IHttpRequestParsePostPlugin parsePostPlugin)
            {
                (requestStatusCode, errorMessage) = await parsePostPlugin.ParseAsync(httpContext.Request, requestMessage);

                if (requestMessage == null || requestStatusCode != StatusCode.OK)
                {
                    await SendErrorResponse(errorProcess, httpContext.Response, selectedEncoding, errorMessage ?? string.Empty, requestStatusCode);
                    return;
                }
            }

            var serverCallContext = new HttpApiServerCallContext(httpContext, _unaryMethodInvoker.Method.FullName);

            TResponse responseMessage;
            try
            {
                responseMessage = await _unaryMethodInvoker.Invoke(httpContext, serverCallContext, (TRequest)requestMessage);
            }
            catch (Exception ex)
            {
                StatusCode statusCode;
                string message;

                if (ex is RpcException rpcException)
                {
                    message = rpcException.Message;
                    statusCode = rpcException.StatusCode;
                }
                else
                {
                    // TODO - Add option for detailed error messages
                    message = "Exception was thrown by handler.";
                    statusCode = StatusCode.Unknown;
                }

                await SendErrorResponse(errorProcess, httpContext.Response, selectedEncoding, message, statusCode);
                return;
            }

            if (serverCallContext.Status.StatusCode != StatusCode.OK)
            {
                await SendErrorResponse(errorProcess, httpContext.Response, selectedEncoding, serverCallContext.Status.ToString(), serverCallContext.Status.StatusCode);
                return;
            }

            await SendResponse(outputProcess, plugin, httpContext.Response, selectedEncoding, responseMessage);
        }

        private async Task<(IMessage requestMessage, StatusCode statusCode, string errorMessage)> CreateMessage(HttpRequest request)
        {
            var method = request.Method.ToLower();
            var contentType = "";

            if (method == "post" || method == "put" || method == "patch" || method == "delete")
            {
                if (!string.IsNullOrEmpty(request.ContentType))
                {
                    contentType = request.ContentType.ToLower().Split(';')[0];
                }

                if (!AllowContentTypes.Contains(contentType))
                {
                    return (null, StatusCode.InvalidArgument, "Request content-type is invalid.");
                }
            }

            IMessage requestMessage;

            if (contentType == "application/x-www-form-urlencoded" || contentType == "multipart/form-data")
            {
                requestMessage = (IMessage)Activator.CreateInstance<TRequest>();

                var form = await request.ReadFormAsync();

                foreach (var item in form)
                {
                    var pathDescriptors = GetPathDescriptors(requestMessage, item.Key);

                    if (pathDescriptors != null)
                    {                       

                        object value = item.Value.Count == 1 ? (object)item.Value[0] : item.Value;
                        ServiceDescriptorHelpers.RecursiveSetValue(requestMessage, pathDescriptors, value);
                    }
                }
            }
            else if (contentType.StartsWith("application/json"))
            {
                
                var encoding = RequestEncoding.SelectCharacterEncoding(request);
                // TODO: Handle unsupported encoding

                using (var requestReader = new HttpRequestStreamReader(request.Body, encoding))
                {
                    try
                    {
                        var body = await requestReader.ReadToEndAsync();
                        requestMessage = (IMessage)_jsonParser.FromJson<TRequest>(body);
                    }
                    catch (InvalidJsonException)
                    {
                        return (null, StatusCode.InvalidArgument, "Request JSON payload is not correctly formatted.");
                    }
                    catch (InvalidProtocolBufferException exception)
                    {
                        return (null, StatusCode.InvalidArgument, exception.Message);
                    }
                }
            }
            else
            {
                requestMessage = (IMessage)Activator.CreateInstance<TRequest>();
            }
            
            foreach (var kv in request.RouteValues)
            {
                var pathDescriptors = GetPathDescriptors(requestMessage, kv.Key);

                if (pathDescriptors != null)
                {
                    ServiceDescriptorHelpers.RecursiveSetValue(requestMessage, pathDescriptors, kv.Value);
                }
            }
            foreach (var item in request.Query)
            {
                var pathDescriptors = GetPathDescriptors(requestMessage, item.Key);

                if (pathDescriptors != null)
                {
                    object value = item.Value.Count == 1 ? (object)item.Value[0] : item.Value;
                    ServiceDescriptorHelpers.RecursiveSetValue(requestMessage, pathDescriptors, value);
                }
            }
            return (requestMessage, StatusCode.OK, null);
        }

        private List<FieldDescriptor> GetPathDescriptors(IMessage requestMessage, string path)
        {
            return _pathDescriptorsCache.GetOrAdd(path, p =>
            {
                ServiceDescriptorHelpers.TryResolveDescriptors(requestMessage.Descriptor, p, out var pathDescriptors);
                return pathDescriptors;
            });
        }

        private async Task SendResponse(IHttpApiOutputProcess outputProcess, IHttpPlugin plugin, HttpResponse response, Encoding encoding, TResponse message)
        {
            object responseBody = message;
            var processed = false;
            if (plugin != null && plugin is IHttpOutputProcessPlugin outputProcessPlugin)
            {
                processed = await outputProcessPlugin.ProcessAsync(response, encoding, responseBody);
            }
            if (processed)
            {
                return;
            }

            if (outputProcess != null)
            {
                (processed, responseBody) = await outputProcess.ProcessAsync(response, encoding, responseBody);
            }
            if (processed)
            {
                return;
            }
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = "application/json";

            await WriteResponseMessage(response, encoding, responseBody);
        }

        private async Task SendErrorResponse(IHttpApiErrorProcess errorProcess, HttpResponse response, Encoding encoding, string message, StatusCode statusCode)
        {
            var e = new Error
            {
                Error_ = message,
                Message = message,
                Code = (int)statusCode
            };

            var processed = false;

            if (errorProcess != null)
            {
                processed = await errorProcess.ProcessAsync(response, encoding, e);
            }
            if (processed)
            {
                return;
            }

            response.StatusCode = MapStatusCodeToHttpStatus(statusCode);
            response.ContentType = "application/json";

            await WriteResponseMessage(response, encoding, e);
        }

        private async Task WriteResponseMessage(HttpResponse response, Encoding encoding, object responseBody)
        {
            using (var writer = new HttpResponseStreamWriter(response.Body, encoding))
            {
                if (responseBody is IMessage responseMessage)
                {
                    _jsonFormatter.Format(responseMessage, writer);
                }
                else
                {
                    _jsonFormatter.WriteValue(writer, responseBody);
                }

                // Perf: call FlushAsync to call WriteAsync on the stream with any content left in the TextWriter's
                // buffers. This is better than just letting dispose handle it (which would result in a synchronous
                // write).
                await writer.FlushAsync();
            }
        }

        private static int MapStatusCodeToHttpStatus(StatusCode statusCode)
        {
            switch (statusCode)
            {
                case StatusCode.OK:
                    return StatusCodes.Status200OK;

                case StatusCode.Cancelled:
                    return StatusCodes.Status408RequestTimeout;

                case StatusCode.Unknown:
                    return StatusCodes.Status500InternalServerError;

                case StatusCode.InvalidArgument:
                    return StatusCodes.Status400BadRequest;

                case StatusCode.DeadlineExceeded:
                    return StatusCodes.Status504GatewayTimeout;

                case StatusCode.NotFound:
                    return StatusCodes.Status404NotFound;

                case StatusCode.AlreadyExists:
                    return StatusCodes.Status409Conflict;

                case StatusCode.PermissionDenied:
                    return StatusCodes.Status403Forbidden;

                case StatusCode.Unauthenticated:
                    return StatusCodes.Status401Unauthorized;

                case StatusCode.ResourceExhausted:
                    return StatusCodes.Status429TooManyRequests;

                case StatusCode.FailedPrecondition:
                    // Note, this deliberately doesn't translate to the similarly named '412 Precondition Failed' HTTP response status.
                    return StatusCodes.Status400BadRequest;

                case StatusCode.Aborted:
                    return StatusCodes.Status409Conflict;

                case StatusCode.OutOfRange:
                    return StatusCodes.Status400BadRequest;

                case StatusCode.Unimplemented:
                    return StatusCodes.Status501NotImplemented;

                case StatusCode.Internal:
                    return StatusCodes.Status500InternalServerError;

                case StatusCode.Unavailable:
                    return StatusCodes.Status503ServiceUnavailable;

                case StatusCode.DataLoss:
                    return StatusCodes.Status500InternalServerError;
            }

            return StatusCodes.Status500InternalServerError;
        }
    }
}