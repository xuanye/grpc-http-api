﻿using System;
using System.Collections.Generic;
using System.Linq;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace Grape.Grpc.HttpApi.Internal
{
    internal class InterceptorPipelineBuilder<TRequest, TResponse>
       where TRequest : class
       where TResponse : class
    {
        private readonly List<InterceptorActivatorHandle> _interceptors;

        public InterceptorPipelineBuilder(IReadOnlyList<InterceptorRegistration> interceptors)
        {
            _interceptors = interceptors.Select(i => new InterceptorActivatorHandle(i)).ToList();
        }

        public ClientStreamingServerMethod<TRequest, TResponse> ClientStreamingPipeline(ClientStreamingServerMethod<TRequest, TResponse> innerInvoker)
        {
            return BuildPipeline(innerInvoker, BuildInvoker);

            static ClientStreamingServerMethod<TRequest, TResponse> BuildInvoker(InterceptorActivatorHandle interceptorActivatorHandle, ClientStreamingServerMethod<TRequest, TResponse> next)
            {
                return async (requestStream, context) =>
                {
                    var serviceProvider = context.GetHttpContext().RequestServices;
                    var interceptorActivator = interceptorActivatorHandle.GetActivator(serviceProvider);
                    var interceptorHandle = CreateInterceptor(interceptorActivatorHandle, interceptorActivator, serviceProvider);

                    try
                    {
                        return await interceptorHandle.Instance.ClientStreamingServerHandler(requestStream, context, next);
                    }
                    finally
                    {
                        await interceptorActivator.ReleaseAsync(interceptorHandle);
                    }
                };
            }
        }

        internal DuplexStreamingServerMethod<TRequest, TResponse> DuplexStreamingPipeline(DuplexStreamingServerMethod<TRequest, TResponse> innerInvoker)
        {
            return BuildPipeline(innerInvoker, BuildInvoker);

            static DuplexStreamingServerMethod<TRequest, TResponse> BuildInvoker(InterceptorActivatorHandle interceptorActivatorHandle, DuplexStreamingServerMethod<TRequest, TResponse> next)
            {
                return async (requestStream, responseStream, context) =>
                {
                    var serviceProvider = context.GetHttpContext().RequestServices;
                    var interceptorActivator = interceptorActivatorHandle.GetActivator(serviceProvider);
                    var interceptorHandle = CreateInterceptor(interceptorActivatorHandle, interceptorActivator, serviceProvider);

                    try
                    {
                        await interceptorHandle.Instance.DuplexStreamingServerHandler(requestStream, responseStream, context, next);
                    }
                    finally
                    {
                        await interceptorActivator.ReleaseAsync(interceptorHandle);
                    }
                };
            }
        }

        internal ServerStreamingServerMethod<TRequest, TResponse> ServerStreamingPipeline(ServerStreamingServerMethod<TRequest, TResponse> innerInvoker)
        {
            return BuildPipeline(innerInvoker, BuildInvoker);

            static ServerStreamingServerMethod<TRequest, TResponse> BuildInvoker(InterceptorActivatorHandle interceptorActivatorHandle, ServerStreamingServerMethod<TRequest, TResponse> next)
            {
                return async (request, responseStream, context) =>
                {
                    var serviceProvider = context.GetHttpContext().RequestServices;
                    var interceptorActivator = interceptorActivatorHandle.GetActivator(serviceProvider);
                    var interceptorHandle = CreateInterceptor(interceptorActivatorHandle, interceptorActivator, serviceProvider);

                    try
                    {
                        await interceptorHandle.Instance.ServerStreamingServerHandler(request, responseStream, context, next);
                    }
                    finally
                    {
                        await interceptorActivator.ReleaseAsync(interceptorHandle);
                    }
                };
            }
        }

        internal UnaryServerMethod<TRequest, TResponse> UnaryPipeline(UnaryServerMethod<TRequest, TResponse> innerInvoker)
        {
            return BuildPipeline(innerInvoker, BuildInvoker);

            static UnaryServerMethod<TRequest, TResponse> BuildInvoker(InterceptorActivatorHandle interceptorActivatorHandle, UnaryServerMethod<TRequest, TResponse> next)
            {
                return async (request, context) =>
                {
                    var serviceProvider = context.GetHttpContext().RequestServices;
                    var interceptorActivator = interceptorActivatorHandle.GetActivator(serviceProvider);
                    var interceptorHandle = CreateInterceptor(interceptorActivatorHandle, interceptorActivator, serviceProvider);

                    try
                    {
                        return await interceptorHandle.Instance.UnaryServerHandler(request, context, next);
                    }
                    finally
                    {
                        await interceptorActivator.ReleaseAsync(interceptorHandle);
                    }
                };
            }
        }

        private T BuildPipeline<T>(T innerInvoker, Func<InterceptorActivatorHandle, T, T> wrapInvoker)
        {
            // The inner invoker will create the service instance and invoke the method
            var resolvedInvoker = innerInvoker;

            // The list is reversed during construction so the first interceptor is built last and invoked first
            for (var i = _interceptors.Count - 1; i >= 0; i--)
            {
                resolvedInvoker = wrapInvoker(_interceptors[i], resolvedInvoker);
            }

            return resolvedInvoker;
        }

        private static GrpcActivatorHandle<Interceptor> CreateInterceptor(
            InterceptorActivatorHandle interceptorActivatorHandle,
            IGrpcInterceptorActivator interceptorActivator,
            IServiceProvider serviceProvider)
        {
            var interceptorHandle = interceptorActivator.Create(serviceProvider, interceptorActivatorHandle.Registration);

            if (interceptorHandle.Instance == null)
            {
                throw new InvalidOperationException($"Could not construct Interceptor instance for type {interceptorActivatorHandle.Registration.Type.FullName}");
            }

            return interceptorHandle;
        }

        private class InterceptorActivatorHandle
        {
            public InterceptorRegistration Registration { get; }

            private IGrpcInterceptorActivator _interceptorActivator;

            public InterceptorActivatorHandle(InterceptorRegistration interceptorRegistration)
            {
                Registration = interceptorRegistration;
            }

            public IGrpcInterceptorActivator GetActivator(IServiceProvider serviceProvider)
            {
                // Not thread safe. Side effect is resolving the service twice.
                if (_interceptorActivator == null)
                {
                    var activatorType = typeof(IGrpcInterceptorActivator<>).MakeGenericType(Registration.Type);
                    _interceptorActivator = (IGrpcInterceptorActivator)serviceProvider.GetRequiredService(activatorType);
                }

                return _interceptorActivator;
            }
        }
    }
}
