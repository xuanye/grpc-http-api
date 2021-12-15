// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Grape.Grpc.HttpApi.Swagger;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the gRPC HTTP API services.
    /// </summary>
    public static class GrpcSwaggerServiceExtensions
    {
        /// <summary>
        /// Adds gRPC HTTP API services to the specified <see cref="IServiceCollection" />.
        /// </summary>   
        public static IServiceCollection AddGrpcSwagger(this IServiceCollection services, Action<SwaggerOptions> configureOptions =null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddGrpcHttpApi();

            services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, GrpcHttpApiDescriptionProvider>());

            // Register default description provider in case MVC is not registered
            services.TryAddSingleton<IApiDescriptionGroupCollectionProvider>(serviceProvider =>
            {
                var actionDescriptorCollectionProvider = serviceProvider.GetService<IActionDescriptorCollectionProvider>();
                var apiDescriptionProvider = serviceProvider.GetServices<IApiDescriptionProvider>();

                return new ApiDescriptionGroupCollectionProvider(
                    actionDescriptorCollectionProvider ?? new EmptyActionDescriptorCollectionProvider(),
                    apiDescriptionProvider);
            });

            if(configureOptions !=null)
                services.Configure(configureOptions);

            services.TryAddSingleton<ISwaggerProvider, DefaultSwaggerProvider>();

            return services;
        }

        // Dummy type that is only used if MVC is not registered in the app
        private class EmptyActionDescriptorCollectionProvider : IActionDescriptorCollectionProvider
        {
            public ActionDescriptorCollection ActionDescriptors { get; } = new ActionDescriptorCollection(new List<ActionDescriptor>(), 1);
        }
    }
}
