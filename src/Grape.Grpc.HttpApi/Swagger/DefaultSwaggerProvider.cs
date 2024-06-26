﻿using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Grape.Grpc.HttpApi.Swagger
{
    public class DefaultSwaggerProvider : ISwaggerProvider
    {
        private static SwaggerInfo _swaggerInfoInstance;

        private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionGroup;
        private readonly SwaggerOptions _config;
        private XmlCommentResolver _resolver;

        public DefaultSwaggerProvider(IApiDescriptionGroupCollectionProvider  apiDescriptionGroup, IOptions<SwaggerOptions> optionsAccessor)
        {
            _apiDescriptionGroup = apiDescriptionGroup;
            _config = optionsAccessor?.Value ?? new SwaggerOptions();
        }
     
        public SwaggerInfo GetSwaggerInfo()
        {
            if(_swaggerInfoInstance != null)
            {
                return _swaggerInfoInstance;
            }
            InitialSwaggerInfo();
            return _swaggerInfoInstance;
        }

        private void InitialSwaggerInfo()
        {
            _swaggerInfoInstance = new SwaggerInfo
            {
                Host = _config.Host,
                Info = _config.ApiInfo ?? new SwaggerApiInfo(),
                BasePath = _config.BasePath ?? "/",
                Paths = new Dictionary<string, Dictionary<string, SwaggerMethod>>(),
                Definitions = new Dictionary<string, SwaggerDefinition>(),
                Tags = new List<SwaggerTag>()
            };

            var applicableApiDescriptions = _apiDescriptionGroup.ApiDescriptionGroups.Items
              .SelectMany(group => group.Items);

            var firstApi = applicableApiDescriptions.FirstOrDefault();
            if (firstApi == null)
            {
                return ;
            }
            var grpcHttpMetadata = firstApi.GetProperty<GrpcHttpMetadata>();
            if(grpcHttpMetadata == null)
            {
                return;
            }
            var firstMethod = grpcHttpMetadata.MethodDescriptor;

            if (_resolver == null)
            {
                var xmlFile = $"{firstMethod.InputType.ClrType.Assembly.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                _resolver = new XmlCommentResolver(new List<string>() { xmlPath });
            }
            GenerateApiPaths(applicableApiDescriptions);
        }

        private void GenerateApiPaths(IEnumerable<ApiDescription> applicableApiDescriptions)
        {
            foreach(var apiDescription in applicableApiDescriptions)
            {
                GenerateApiPath(apiDescription);
            }
        }

        private void GenerateApiPath(ApiDescription apiDescription)
        {
            var swagger = _swaggerInfoInstance;

            var grpcHttpMetadata = apiDescription.GetProperty<GrpcHttpMetadata>();

            var tagName = grpcHttpMetadata.MethodDescriptor.Service.Name;

            if (!_swaggerInfoInstance.Tags.Exists(x => x.Name == tagName))
            {
                _swaggerInfoInstance.Tags.Add(new SwaggerTag
                {
                    Name = tagName,
                    Description = _resolver.GetTypeComment(grpcHttpMetadata.HanderServiceType)
                }); 
            }

            var acceptVerb = apiDescription.HttpMethod.ToLower();
            var pathMethod = CreateSwaggerMethod(acceptVerb);

            pathMethod.Tags = new List<string> {tagName};

            pathMethod.Version = !string.IsNullOrEmpty(grpcHttpMetadata.HttpApiOption.Version)? grpcHttpMetadata.HttpApiOption.Version : "1.0";
            pathMethod.Summary = $"{tagName}.{grpcHttpMetadata.MethodDescriptor.Name}";


            apiDescription.SupportedRequestFormats.ForEach(requestFormat => pathMethod.Consumes.Add(requestFormat.MediaType));
           

            pathMethod.OperationId = grpcHttpMetadata.MethodDescriptor.Name;

            pathMethod.Description = _resolver.GetMethodComment(grpcHttpMetadata.HanderMethod);

            //parameters
            apiDescription.ParameterDescriptions.ForEach(paramter => {
                                
                var name = paramter.ModelMetadata.Name;
                if (_config.IngoreFields.Contains(name))
                {
                     return;
                }
                var p = paramter.ModelMetadata.ContainerType.GetProperty(name, BindingFlags.Public |BindingFlags.Instance);
                
                if (p == null)
                {
                    return;
                }
                var apiParameter = new SwaggerApiParameters
                {                   
                    Name = paramter.Name,
                    Required = paramter.IsRequired,
                    DefaultValue = paramter.DefaultValue?.ToString(),
                    Description = _resolver.GetMemberInfoComment(p)
                };

                if (p.PropertyType == typeof(string) || p.PropertyType.IsValueType)
                {
                    var (typeName,format) = GetSwaggerType(p.PropertyType);
                    apiParameter.Type = typeName;
                    apiParameter.Format = format;
                    apiParameter.In = ConvertBindSource(paramter.Source);
                }
                else
                {
                    apiParameter.In = "body";
                    apiParameter.Schema = GetSwaggerItemSchema(p.PropertyType);
                }
                pathMethod.Parameters.Add(apiParameter);
            });

            //response          
            apiDescription.SupportedResponseTypes.ForEach(apiResponse =>
            {
                apiResponse.ApiResponseFormats.ForEach(fomart =>
                {                  
                    pathMethod.Produces.Add(fomart.MediaType);

                });
              
                var responseType = apiResponse.Type;
                var swaggerApiResponse = new SwaggerApiResponse
                {
                    Description = _resolver.GetTypeComment(responseType),
                    Schema = GetSwaggerItemSchema(responseType)
                };
                pathMethod.Responses.Add(apiResponse.StatusCode.ToString(), swaggerApiResponse);
            });

            if (!swagger.Paths.ContainsKey(apiDescription.RelativePath))
            {
                swagger.Paths.Add(apiDescription.RelativePath, new Dictionary<string, SwaggerMethod> { { acceptVerb, pathMethod } });
            }
            else
            {
                var pathItem = swagger.Paths[apiDescription.RelativePath];
                if (!pathItem.ContainsKey(acceptVerb))
                {
                    pathItem.Add(acceptVerb, pathMethod);
                }
            }
        }

        private SwaggerItemSchema GetSwaggerItemSchema(Type type)
        {

            if (type.IsArray && type.HasElementType)
            {
                SwaggerArrayItemSchema arrayItem = new SwaggerArrayItemSchema();
                arrayItem.Items.Add(new SwaggerSingleItemSchema
                {
                    Ref = "#/definitions/" + type.GetElementType().Name
                });
                CreateSwaggerDefinition(type.GetElementType().Name, type.GetElementType());
                return arrayItem;
            }

            if (type == typeof(string) || type.IsValueType) //string or valueType
            {
                var (typeName, _) = GetSwaggerType(type);
              

                SwaggerSingleItemSchema valueItem = new SwaggerSingleItemSchema { Ref = typeName };

                CreateSwaggerDefinition(type.Name, type);

                return valueItem;
            }

            if (
               IsCollectionType(type)
            )
            {
                SwaggerArrayItemSchema arrayItem = new SwaggerArrayItemSchema();

                if (type.IsGenericType)
                {
                    arrayItem.Items.Add(new SwaggerSingleItemSchema
                    {
                        Ref = "#/definitions/" + type.GenericTypeArguments[0].Name
                    });
                    CreateSwaggerDefinition(type.GenericTypeArguments[0].Name, type.GenericTypeArguments[0]);

                }
                else
                {
                    arrayItem.Items.Add(new SwaggerSingleItemSchema
                    {
                        Ref = "#/definitions/Object"
                    });
                }
                return arrayItem;
            }

            SwaggerSingleItemSchema singleItem = new SwaggerSingleItemSchema { Ref = "#/definitions/" + type.Name };

            CreateSwaggerDefinition(type.Name, type);

            return singleItem;
        }

        private bool IsCollectionType(Type type)
        {
            return type.GetInterface(nameof(ICollection)) != null;
        }

         private void CreateSwaggerDefinition(string name, Type definitionType)
        {
            if (_swaggerInfoInstance.Definitions.ContainsKey(name))
            {
                return;
            }
            if (definitionType == typeof(string) || definitionType.IsValueType) //string or valueType
            {
                return;
            }

            if ("Type".Equals(name))
            {
                return;
            }

            SwaggerDefinition definition = new SwaggerDefinition
            {
                Type = "object",
                Properties = new Dictionary<string, SwaggerPropertyDefinition>()
            };


            _swaggerInfoInstance.Definitions.Add(name, definition);

            var properties = definitionType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            properties.ForEach(p =>
            {
                SwaggerPropertyDefinition pd = new SwaggerPropertyDefinition
                {
                    Description = _resolver.GetMemberInfoComment(p)
                };

                if (p.PropertyType == typeof(string) || p.PropertyType.IsValueType)
                {
                    var (type,format) =GetSwaggerType(p.PropertyType);
                    pd.Type = type;
                    pd.Format = format;
                }
                else if (p.PropertyType.IsArray && p.PropertyType.HasElementType)
                {
                    pd.Type = "array";
                    pd.Items = GetSwaggerItemSchema(p.PropertyType.GetElementType());
                }
                else if (
                   IsCollectionType(p.PropertyType)
                    )
                {

                    if (p.PropertyType.IsGenericType)
                    {
                        pd.Type = "array";
                        pd.Items = GetSwaggerItemSchema(p.PropertyType.GenericTypeArguments[0]);
                    }
                    else
                    {
                        pd.Type = "array";
                    }
                }
                else
                {
                    //Console.WriteLine(p.PropertyType.Name);
                    pd.Ref = "#/definitions/" + p.PropertyType.Name;
                    CreateSwaggerDefinition(p.PropertyType.Name, p.PropertyType);
                }

                definition.Properties.Add(p.Name.ToCamelCase(), pd);
            });


        }

      
        private (string, string) GetSwaggerType(Type type)
        {         

            if (type == typeof(string))
            {              
                return ("string", null);
            }

            if (type == typeof(int) || type == typeof(uint) || type.IsEnum)
            {
                return ("integer","int32");
            }
            if (type == typeof(long) || type == typeof(ulong))
            {
                return ("integer", "int64");
            }
            if (type == typeof(short) || type == typeof(ushort))
            {
                return ("integer", "int16");
            }
           
            if (type == typeof(bool))
            {
                return ("boolean",null);
            }

            if (type == typeof(float) || type == typeof(decimal) || type == typeof(double) )
            {
                return ("number",null);
            }

            if(type.IsGenericType) //NullAble valueType
            {
                return GetSwaggerType(type.GetGenericArguments()[0]);
            }

            return  ("string", null);

        }

        private string ConvertBindSource(BindingSource source)
        {
            if(source == BindingSource.Path)
            {
                return "path";
            }
            else if( source == BindingSource.Form)
            {
                return "formData";
            }

            return "query";
        }

        private SwaggerMethod CreateSwaggerMethod(string verb)
        {
            return verb switch
            {
                "get" => new SwaggerGetMethod(),
                "post" => new SwaggerPostMethod(),
                "put" => new SwaggerPutMethod(),
                "delete" => new SwaggerDeleteMethod(),
                "patch" => new SwaggerPatchMethod(),
                _ => new SwaggerGetMethod(),
            };
        }

    }
}
