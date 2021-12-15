using Grape.Grpc.HttpApi.Swagger;
using Grape.Grpc.HttpApi.UnitTests.TestObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

using Xunit;

namespace Grape.Grpc.HttpApi.UnitTests
{
    public class GrpcSwaggerGenerateTests
    {
        [Fact]
        public void AddGrpcSwagger_GrpcServiceRegistered_ReturnSwaggerWithGrpcOperation()
        {
            // Arrange & Act
            var services = new ServiceCollection();
            services.AddGrpcSwagger();          
            services.AddRouting();
            services.AddLogging();
            services.AddSingleton<IWebHostEnvironment, TestWebHostEnvironment>();
            var serviceProvider = services.BuildServiceProvider();
            var app = new ApplicationBuilder(serviceProvider);

            app.UseRouting();
            app.UseEndpoints(c =>
            {
                c.MapGrpcService<GrpcHttpApiGreeterService>();
            });

            var swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();
            var swagger = swaggerProvider.GetSwaggerInfo();

            // Assert
            Assert.NotNull(swagger);
            Assert.NotNull(swagger.Paths);
            Assert.Equal(2, swagger.Paths.Count);

            Assert.NotNull(swagger.Paths["/api/greeter/{name}"]);
            var sayHelloApi = swagger.Paths["/api/greeter/{name}"]["get"];

            Assert.NotNull(sayHelloApi);
            Assert.Single(sayHelloApi.Tags);
            Assert.Equal("HttpApiGreeterService", sayHelloApi.Tags[0]);
            Assert.Equal("1.0", sayHelloApi.Version);
            Assert.Equal("HttpApiGreeterService.SayHello", sayHelloApi.Summary);
            Assert.Equal("SayHello Action", sayHelloApi.Description);
            Assert.Equal("SayHello", sayHelloApi.OperationId);
            Assert.Empty(sayHelloApi.Consumes);
            Assert.Single(sayHelloApi.Produces);
            Assert.Equal("application/json", sayHelloApi.Produces[0]);
            
            Assert.Equal(18, sayHelloApi.Parameters.Count);
        

            //route parameter will be set first
            var nameParameter = sayHelloApi.Parameters[0];

            Assert.NotNull(nameParameter);
            Assert.Equal("name", nameParameter.Name);
            Assert.Equal("string", nameParameter.Type);
            Assert.Equal("path", nameParameter.In);
            Assert.True(nameParameter.Required);



            var singleInt32Paramter = sayHelloApi.Parameters[1];
            Assert.NotNull(singleInt32Paramter);
            Assert.Equal("singleInt32", singleInt32Paramter.Name);
            Assert.Equal("integer", singleInt32Paramter.Type);
            Assert.Equal("query", singleInt32Paramter.In);
            Assert.Equal("int32", singleInt32Paramter.Format);



            var subMessageParamter = sayHelloApi.Parameters[17];

            Assert.NotNull(subMessageParamter);
            Assert.Equal("subMessage", subMessageParamter.Name);
            Assert.Equal("body", subMessageParamter.In);

            Assert.NotNull(subMessageParamter.Schema);


            Assert.IsType<SwaggerSingleItemSchema>(subMessageParamter.Schema);

        
            var subMessageSchema = subMessageParamter.Schema as SwaggerSingleItemSchema;
            Assert.NotNull(subMessageSchema);

            Assert.Equal("#/definitions/SubMessage", subMessageSchema.Ref);

            //response assert

            Assert.NotNull(sayHelloApi.Responses);
            Assert.Single(sayHelloApi.Responses);
            Assert.True(sayHelloApi.Responses.ContainsKey("200"));


            var successRes = sayHelloApi.Responses["200"];

            Assert.NotNull(successRes);
            Assert.NotNull(successRes.Schema);
            Assert.IsType<SwaggerSingleItemSchema>(successRes.Schema);
          
            var responseSchema = successRes.Schema as SwaggerSingleItemSchema;
            Assert.NotNull(responseSchema);
            Assert.Equal("#/definitions/HelloReply", responseSchema.Ref);


            Assert.NotNull(swagger.Definitions);
            Assert.True(swagger.Definitions.ContainsKey("HelloReply"));         

            var helloReplySchema = swagger.Definitions["HelloReply"];

            Assert.NotNull(helloReplySchema);

            Assert.NotNull(helloReplySchema.Properties);
            Assert.Equal(3, helloReplySchema.Properties.Count);
            Assert.True(helloReplySchema.Properties.ContainsKey("message"));
            Assert.True(helloReplySchema.Properties.ContainsKey("listMessage"));
            Assert.True(helloReplySchema.Properties.ContainsKey("nullableMessage"));

         

            var messageProperty = helloReplySchema.Properties["message"];
            var listMessageProperty = helloReplySchema.Properties["listMessage"];
            var nullableMessageProperty = helloReplySchema.Properties["nullableMessage"];

            Assert.NotNull(messageProperty);
            Assert.NotNull(listMessageProperty);
            Assert.NotNull(nullableMessageProperty);

            Assert.Equal("string", messageProperty.Type);
            Assert.Equal("message comment", messageProperty.Description);


            Assert.Equal("array", listMessageProperty.Type);
            Assert.Equal("list message comment", listMessageProperty.Description);

            Assert.Equal("string", nullableMessageProperty.Type);
            Assert.Equal("nullable message comment", nullableMessageProperty.Description);

        }


        private class TestWebHostEnvironment : IWebHostEnvironment
        {
            public IFileProvider WebRootFileProvider { get; set; }
            public string WebRootPath { get; set; }
            public string ApplicationName { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; }
            public string ContentRootPath { get; set; }
            public string EnvironmentName { get; set; }
        }

    }
}
