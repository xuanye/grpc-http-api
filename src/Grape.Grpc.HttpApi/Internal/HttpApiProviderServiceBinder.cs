using Google.Protobuf.Reflection;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Log = Grape.Grpc.HttpApi.Internal.HttpApiProviderServiceBinderLog;

namespace Grape.Grpc.HttpApi.Internal
{
    internal class HttpApiProviderServiceBinder<TService> : ServiceBinderBase where TService : class
    {
        private readonly ServiceMethodProviderContext<TService> _context;
        private readonly Type _declaringType;
        private readonly ServiceDescriptor _serviceDescriptor;
        private readonly IGrpcServiceActivator<TService> _serviceActivator;
        private readonly GrpcServiceOptions _globalOptions;
        private readonly GrpcServiceOptions<TService> _serviceOptions;
        private readonly GrpcHttpApiOptions _grpcHttpApiOptions;
        private readonly ILogger _logger;
        internal HttpApiProviderServiceBinder(
            ServiceMethodProviderContext<TService> context, 
            Type declaringType, 
            ServiceDescriptor serviceDescriptor,
            GrpcServiceOptions globalOptions,
            GrpcServiceOptions<TService> serviceOptions,
            ILoggerFactory loggerFactory,
            IGrpcServiceActivator<TService> serviceActivator,
            GrpcHttpApiOptions grpcHttpApiOptions
            )
        {
            _context = context;
            _declaringType = declaringType;
            _serviceDescriptor = serviceDescriptor;
            _globalOptions = globalOptions;
            _serviceOptions = serviceOptions;
            _serviceActivator = serviceActivator;
            _grpcHttpApiOptions = grpcHttpApiOptions;
            _logger = loggerFactory.CreateLogger<HttpApiProviderServiceBinder<TService>>();
        }
       
        public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, ClientStreamingServerMethod<TRequest, TResponse> handler)
        {
            if (TryGetMethodDescriptor(method.Name, out var methodDescriptor) &&
                ServiceDescriptorHelpers.TryGetHttpApiOption(methodDescriptor, out _))
            {
                Log.StreamingMethodNotSupported(_logger, method.Name, typeof(TService));
            }
        }    
        
        public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, DuplexStreamingServerMethod<TRequest, TResponse> handler)
        {
            if (TryGetMethodDescriptor(method.Name, out var methodDescriptor) &&
                ServiceDescriptorHelpers.TryGetHttpApiOption(methodDescriptor, out _))
            {
                Log.StreamingMethodNotSupported(_logger, method.Name, typeof(TService));
            }
        }
        
        public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, ServerStreamingServerMethod<TRequest, TResponse> handler)
        {
            if (TryGetMethodDescriptor(method.Name, out var methodDescriptor) &&
                ServiceDescriptorHelpers.TryGetHttpApiOption(methodDescriptor, out _))
            {
                Log.StreamingMethodNotSupported(_logger, method.Name, typeof(TService));
            }
        }

        public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse> handler)
        {
            if (TryGetMethodDescriptor(method.Name, out var methodDescriptor))
            {
                if (ServiceDescriptorHelpers.TryGetHttpApiOption(methodDescriptor, out var httpApiOption))
                {
                    ProcessHttpApiOption(method, methodDescriptor, httpApiOption);
                }
            }
            else
            {
                Log.MethodDescriptorNotFound(_logger, method.Name, typeof(TService));
            }
        }

        private void ProcessHttpApiOption<TRequest, TResponse>(Method<TRequest, TResponse> method, MethodDescriptor methodDescriptor, HttpApiOption httpApiOption) 
            where TRequest : class 
            where TResponse : class
        {
            var reqMethod = httpApiOption.Method.ToLower();
            if(reqMethod =="any" || reqMethod == "all")
            {
                var anyMethods =new string[] { "get", "post" };

                anyMethods.ForEach(m =>                {
                    var replicaOption = httpApiOption.Clone();
                    replicaOption.Method = m;
                    AddMethodCore(method, methodDescriptor, replicaOption);
                });
               
            }
            else
            {
                AddMethodCore(method, methodDescriptor, httpApiOption);
            }
            
        }

        private void AddMethodCore<TRequest, TResponse>(Method<TRequest, TResponse> method, MethodDescriptor methodDescriptor, HttpApiOption httpApiOption) 
            where TRequest : class 
            where TResponse : class
        {
            try
            {
                var pattern = httpApiOption.Path;
              
                if (!pattern.StartsWith('/'))
                {
                    // This validation is consistent with grpc-gateway code generation.
                    // We should match their validation to be a good member of the eco-system.
                    throw new InvalidOperationException($"Path template must start with /: {pattern}");
                }

                var (invoker, metadata) = CreateModelCore<UnaryServerMethod<TService, TRequest, TResponse>>(
                    method.Name,
                    new[] { typeof(TRequest), typeof(ServerCallContext) },
                    httpApiOption,
                    methodDescriptor);

                var methodContext = MethodOptions.Create(new[] { _globalOptions, _serviceOptions });

                var routePattern = RoutePatternFactory.Parse(pattern);

                var unaryInvoker = new UnaryServerMethodInvoker<TService, TRequest, TResponse>(invoker, method, methodContext, _serviceActivator);
                var unaryServerCallHandler = new UnaryServerCallHandler<TService, TRequest, TResponse>(unaryInvoker, httpApiOption, _grpcHttpApiOptions);

                _context.AddMethod<TRequest, TResponse>(method, routePattern, metadata, unaryServerCallHandler.HandleCallAsync);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error binding {method.Name} on {typeof(TService).Name} to HTTP API.", ex);
            }
        }

        private bool TryGetMethodDescriptor(string methodName, [NotNullWhen(true)]out MethodDescriptor methodDescriptor)
        {
            methodDescriptor = _serviceDescriptor.Methods.SingleOrDefault(m => m.Name == methodName);
            return methodDescriptor != null;
        }

        private (TDelegate invoker, List<object> metadata) CreateModelCore<TDelegate>(
            string methodName, 
            Type[] methodParameters,
            HttpApiOption httpApiOption,
            MethodDescriptor methodDescriptor) 
            where TDelegate : Delegate
        {
            var handlerMethod = GetMethod(methodName, methodParameters);

            if (handlerMethod == null)
            {
                throw new InvalidOperationException($"Could not find '{methodName}' on {typeof(TService)}.");
            }

            var invoker = (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), handlerMethod);

            var metadata = new List<object>();
            // Add type metadata first so it has a lower priority
            metadata.AddRange(typeof(TService).GetCustomAttributes(inherit: true));
            // Add method metadata last so it has a higher priority
            metadata.AddRange(handlerMethod.GetCustomAttributes(inherit: true));
            metadata.Add(new HttpMethodMetadata(new[] { httpApiOption.Method.ToUpper() }));

            // Add protobuf service method descriptor.
            // Is used by swagger generation to identify gRPC HTTP APIs.
            metadata.Add(new GrpcHttpMetadata(handlerMethod, methodDescriptor, httpApiOption));

            return (invoker, metadata);
        }

        private MethodInfo GetMethod(string methodName, Type[] methodParameters)
        {
            Type currentType = typeof(TService);
            while (currentType != null)
            {
                // Specify binding flags explicitly because we don't want to match static methods.
                var matchingMethod = currentType.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Instance,
                    binder: null,
                    types: methodParameters,
                    modifiers: null);

                if (matchingMethod == null)
                {
                    return null;
                }

                // Validate that the method overrides the virtual method on the base service type.
                // If there is a method with the same name it will hide the base method. Ignore it,
                // and continue searching on the base type.
                if (matchingMethod.IsVirtual)
                {
                    var baseDefinitionMethod = matchingMethod.GetBaseDefinition();
                    if (baseDefinitionMethod.DeclaringType == _declaringType)
                    {
                        return matchingMethod;
                    }
                }

                currentType = currentType.BaseType;
            }

            return null;
        }
        
       
        
    }

    [ExcludeFromCodeCoverage]
    internal static class HttpApiProviderServiceBinderLog
    {
        private static readonly Action<ILogger, string, Type, Exception> _streamingMethodNotSupported =
            LoggerMessage.Define<string, Type>(LogLevel.Warning, new EventId(1, "StreamingMethodNotSupported"), "Unable to bind {MethodName} on {ServiceType} to HTTP API. Streaming methods are not supported.");

        private static readonly Action<ILogger, string, Type, Exception> _methodDescriptorNotFound =
            LoggerMessage.Define<string, Type>(LogLevel.Warning, new EventId(2, "MethodDescriptorNotFound"), "Unable to find method descriptor for {MethodName} on {ServiceType}.");

        public static void StreamingMethodNotSupported(ILogger logger, string methodName, Type serviceType)
        {
            _streamingMethodNotSupported(logger, methodName, serviceType, null);
        }

        public static void MethodDescriptorNotFound(ILogger logger, string methodName, Type serviceType)
        {
            _methodDescriptorNotFound(logger, methodName, serviceType, null);
        }
    }
}
