using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grape.Grpc.HttpApi.UnitTests.TestObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Grape.Grpc.HttpApi;

namespace Grape.Grpc.HttpApi.UnitTests
{
    public class HttpApiServiceMethodProviderTests
    {

        [Fact]
        public void AddMethod_OptionGet_ResolveMethod()
        {
            // arrange & act
            var endpoints = MapEndpoints<GrpcHttpApiGreeterService>();

           
            var endpoint = FindGrpcEndpoint(endpoints, nameof(GrpcHttpApiGreeterService.SayHello));
            var method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>().HttpMethods.Single();

            // assert
            Assert.Equal("GET", method);
            Assert.Equal("/api/greeter/{name}", endpoint.RoutePattern.RawText);
            Assert.Single(endpoint.RoutePattern.Parameters);
            Assert.Equal("name", endpoint.RoutePattern.Parameters[0].Name);
  
        }

        [Fact]
        public void AddMethod_OptionPost_ResolveMethod()
        {
            // arrange & act
            var endpoints = MapEndpoints<GrpcHttpApiGreeterService>();
           
            var endpoint = FindGrpcEndpoint(endpoints, nameof(GrpcHttpApiGreeterService.PostTest));
            var method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>().HttpMethods.Single();

            // assert
            Assert.Equal("POST", method);
            Assert.Equal("/api/greeter", endpoint.RoutePattern.RawText);
            Assert.Empty(endpoint.RoutePattern.Parameters);
    
        }

        [Fact]
        public void AddMethod_NoHttpApiOptionInProto_ThrowNotFoundError()
        {
            // Arrange & Act
            var endpoints = MapEndpoints<GrpcHttpApiGreeterService>();
            Action action = () => FindGrpcEndpoint(endpoints, nameof(GrpcHttpApiGreeterService.NoOption));

            // Assert
            var exception = Assert.Throws<InvalidOperationException>(action);

            Assert.Equal("Couldn't find gRPC endpoint for method NoOption.", exception.Message);
         
        }


        [Fact]
        public void AddMethod_BadPattern_ThrowError()
        {
            // Arrange & Act
            Action action = () => MapEndpoints<GrpcHttpApiInvalidPatternGreeterService>();

            // Assert

            var invalidOperationException = Assert.Throws<InvalidOperationException>(action);

            Assert.Equal("Error binding gRPC service 'GrpcHttpApiInvalidPatternGreeterService'.", invalidOperationException.Message);
            Assert.IsType<TargetInvocationException>(invalidOperationException.InnerException);
            Assert.IsType<InvalidOperationException>(invalidOperationException.InnerException.InnerException);

            Assert.Equal("Error binding BadPattern on GrpcHttpApiInvalidPatternGreeterService to HTTP API.", invalidOperationException.InnerException.InnerException.Message);
            Assert.IsType<InvalidOperationException>(invalidOperationException.InnerException.InnerException.InnerException);
            Assert.Equal("Path template must start with /: api/greeter/{name}", invalidOperationException.InnerException.InnerException.InnerException.Message);
     
        }

        private static RouteEndpoint FindGrpcEndpoint(IReadOnlyList<Endpoint> endpoints, string methodName)
        {
            var e = FindGrpcEndpoints(endpoints, methodName).SingleOrDefault();
            if (e == null)
            {
                throw new InvalidOperationException($"Couldn't find gRPC endpoint for method {methodName}.");
            }

            return e;
        }

        private static List<RouteEndpoint> FindGrpcEndpoints(IReadOnlyList<Endpoint> endpoints, string methodName)
        {
            var e = endpoints
                .Where(e => e.Metadata.GetMetadata<GrpcMethodMetadata>()?.Method.Name == methodName)
                .Cast<RouteEndpoint>()
                .ToList();

            return e;
        }

        private IReadOnlyList<Endpoint> MapEndpoints<TService>()
           where TService : class
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddGrpc();
            serviceCollection.RemoveAll(typeof(IServiceMethodProvider<>));
            serviceCollection.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IServiceMethodProvider<>), typeof(HttpApiServiceMethodProvider<>)));

            IEndpointRouteBuilder endpointRouteBuilder = new TestEndpointRouteBuilder(serviceCollection.BuildServiceProvider());

            endpointRouteBuilder.MapGrpcService<TService>();

            return endpointRouteBuilder.DataSources.Single().Endpoints;
        }
        private class TestEndpointRouteBuilder : IEndpointRouteBuilder
        {
            public ICollection<EndpointDataSource> DataSources { get; }
            public IServiceProvider ServiceProvider { get; }

            public TestEndpointRouteBuilder(IServiceProvider serviceProvider)
            {
                DataSources = new List<EndpointDataSource>();
                ServiceProvider = serviceProvider;
            }

            public IApplicationBuilder CreateApplicationBuilder()
            {
                return new ApplicationBuilder(ServiceProvider);
            }
        }
    }
}
