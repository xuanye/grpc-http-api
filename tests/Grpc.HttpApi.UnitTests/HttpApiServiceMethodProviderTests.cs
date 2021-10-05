using FluentAssertions;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.HttpApi.UnitTests.TestObjects;
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

namespace Grpc.HttpApi.UnitTests
{
    public class HttpApiServiceMethodProviderTests
    {

        [Fact]
        public void AddMethod_OptionGet_ResolveMethod()
        {
            // arrange & act
            var endpoints = MapEndpoints<GrpcHttpApiGreeterService>();

            // assert
            var endpoint = FindGrpcEndpoint(endpoints, nameof(GrpcHttpApiGreeterService.SayHello));
            var method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>().HttpMethods.Single();
            method.Should().Be("GET");
            endpoint.RoutePattern.RawText.Should().Be("/api/greeter/{name}");
            endpoint.RoutePattern.Parameters.Should().ContainSingle();

            endpoint.RoutePattern.Parameters[0].Name.Should().Be("name");
        }

        [Fact]
        public void AddMethod_OptionPost_ResolveMethod()
        {
            // arrange & act
            var endpoints = MapEndpoints<GrpcHttpApiGreeterService>();

            // assert
            var endpoint = FindGrpcEndpoint(endpoints, nameof(GrpcHttpApiGreeterService.PostTest));
            var method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>().HttpMethods.Single();
            method.Should().Be("POST");
            endpoint.RoutePattern.RawText.Should().Be("/api/greeter");
            endpoint.RoutePattern.Parameters.Should().BeEmpty();           
        }

        [Fact]
        public void AddMethod_NoHttpApiOptionInProto_ThrowNotFoundError()
        {
            // Arrange & Act
            var endpoints = MapEndpoints<GrpcHttpApiGreeterService>();
            Action action = () => FindGrpcEndpoint(endpoints, nameof(GrpcHttpApiGreeterService.NoOption));

            // Assert
            action.Should().Throw<InvalidOperationException>().WithMessage("Couldn't find gRPC endpoint for method NoOption.");
        }


        [Fact]
        public void AddMethod_BadPattern_ThrowError()
        {
            // Arrange & Act
            Action action = () => MapEndpoints<GrpcHttpApiInvalidPatternGreeterService>();

            // Assert
            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Error binding gRPC service 'GrpcHttpApiInvalidPatternGreeterService'.")
                .WithInnerException<TargetInvocationException>() //Reflection Invoke Exception
                .WithInnerException<InvalidOperationException>()
                .WithMessage("Error binding BadPattern on GrpcHttpApiInvalidPatternGreeterService to HTTP API.")
                .WithInnerException<InvalidOperationException>()
                .WithMessage("Path template must start with /: api/greeter/{name}");      
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
