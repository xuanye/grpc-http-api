using Google.Protobuf.Reflection;
using Grpc.HttpApi.Internal;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Grpc.HttpApi.Swagger
{
    internal class GrpcHttpApiDescriptionProvider : IApiDescriptionProvider
    {
        private readonly EndpointDataSource _endpointDataSource;

        public GrpcHttpApiDescriptionProvider(EndpointDataSource endpointDataSource)
        {
            _endpointDataSource = endpointDataSource;
        }

        // Executes after ASP.NET Core
        public int Order => -900;

        public void OnProvidersExecuting(ApiDescriptionProviderContext context)
        {
            var endpoints = _endpointDataSource.Endpoints;

            foreach (var endpoint in endpoints)
            {
                if (endpoint is RouteEndpoint routeEndpoint)
                {
                    var grpcMetadata = endpoint.Metadata.GetMetadata<GrpcHttpMetadata>();

                    if (grpcMetadata != null)
                    {
                        var httpRule = grpcMetadata.HttpApiOption;
                        var methodDescriptor = grpcMetadata.MethodDescriptor;

                        var pattern = grpcMetadata.HttpApiOption.Path;
                        var verb = grpcMetadata.HttpApiOption.Method.ToUpper();
                       
                        var apiDescription = CreateApiDescription(routeEndpoint, httpRule, methodDescriptor, pattern, verb);
                        context.Results.Add(apiDescription);
                    }
                }
            }
        }

        private static ApiDescription CreateApiDescription(RouteEndpoint routeEndpoint, HttpApiOption httpRule, MethodDescriptor methodDescriptor, string pattern, string verb)
        {
            var apiDescription = new ApiDescription();
            apiDescription.HttpMethod = verb;
            apiDescription.ActionDescriptor = new ActionDescriptor
            {
                DisplayName = methodDescriptor.Name,
                RouteValues = new Dictionary<string, string>
                {
                    // Swagger uses this to group endpoints together.
                    // Group methods together using the service name.
                    ["controller"] = methodDescriptor.Service.FullName
                }
            };
            apiDescription.RelativePath = pattern.TrimStart('/');

            if (!verb.Equals("get", System.StringComparison.OrdinalIgnoreCase))
            {
                apiDescription.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/x-www-form-urlencoded" });
                apiDescription.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/json" });
            }

        
            apiDescription.SupportedResponseTypes.Add(new ApiResponseType
            {
                ApiResponseFormats = { new ApiResponseFormat { MediaType = "application/json" } },
                ModelMetadata = new GrpcModelMetadata(ModelMetadataIdentity.ForType(methodDescriptor.OutputType.ClrType)),
                StatusCode = 200
            });

            var routeParameters = ServiceDescriptorHelpers.ResolveRouteParameterDescriptors(routeEndpoint.RoutePattern, methodDescriptor.InputType);

            foreach (var routeParameter in routeParameters)
            {
                var field = routeParameter.Value.Last();

                var modelMetadataIdentity = ModelMetadataIdentity.ForProperty(
                        field.ContainingType.ClrType.GetProperty(field.Name.ToPascalCase())
                      , MessageDescriptorHelpers.ResolveFieldType(field)
                      , field.ContainingType.ClrType
                      );
                apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
                {
                    Name = routeParameter.Key,
                    ModelMetadata = new GrpcModelMetadata(modelMetadataIdentity),
                    Source = BindingSource.Path,
                    IsRequired = true,                   
                    DefaultValue = string.Empty
                });
            }
            var fields = methodDescriptor.InputType.Fields.InDeclarationOrder();
            if (verb.Equals("get", System.StringComparison.OrdinalIgnoreCase))
            {               
                foreach(var field in fields)
                {
                  
                    var modelMetadataIdentity = ModelMetadataIdentity.ForProperty(
                          field.ContainingType.ClrType.GetProperty(field.Name.ToPascalCase())
                        , MessageDescriptorHelpers.ResolveFieldType(field)
                        , field.ContainingType.ClrType
                        );
                    apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
                    {
                        Name = field.Name,
                        ModelMetadata = new GrpcModelMetadata(modelMetadataIdentity),
                        Source = BindingSource.Query,
                        IsRequired = field.IsRequired                       
                    });
                }                
            }
            else
            {
                foreach (var field in fields)
                {
                    var modelMetadataIdentity = ModelMetadataIdentity.ForProperty(
                         field.ContainingType.ClrType.GetProperty(field.Name.ToPascalCase())
                       , MessageDescriptorHelpers.ResolveFieldType(field)
                       , field.ContainingType.ClrType
                       );
                    apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
                    {
                        Name = field.Name,
                        ModelMetadata = new GrpcModelMetadata(modelMetadataIdentity),
                        Source = BindingSource.Form,
                        IsRequired = field.IsRequired
                    });
                }
            }          

            return apiDescription;
        }

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
            // no-op
        }
       
    }
}
