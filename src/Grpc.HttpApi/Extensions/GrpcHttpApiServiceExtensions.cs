using Grpc.AspNetCore.Server.Model;
using Grpc.HttpApi;
using Grpc.HttpApi.Contracts;
using Grpc.HttpApi.Implements;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GrpcHttpApiServiceExtensions
    {       
        /// <summary>
        /// Adds gRPC HTTP API services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddGrpcHttpApi(this IServiceCollection services)
        {
         
            services.AddGrpc();
           
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IServiceMethodProvider<>), typeof(HttpApiServiceMethodProvider<>)));

            return services;
        }

        /// <summary>
        /// Adds gRPC HTTP API services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <param name="configureOptions">An <see cref="Action{GrpcHttpApiOptions}"/> to configure the provided <see cref="GrpcHttpApiOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddGrpcHttpApi(this IServiceCollection services, Action<GrpcHttpApiOptions> configureOptions)
        {            
            return services.Configure(configureOptions).AddGrpcHttpApi();
        }
    }
}
