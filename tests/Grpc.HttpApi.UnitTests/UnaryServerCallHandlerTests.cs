using FluentAssertions;
using Google.Protobuf;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.HttpApi.Contracts;
using Grpc.HttpApi.Internal;
using Grpc.HttpApi.UnitTests.TestObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Type = System.Type;

namespace Grpc.HttpApi.UnitTests
{
    public class UnaryServerCallHandlerTests
    {
        [Fact]
        public async Task HandleCallAsync_MatchingRouteValue_SetOnRequestMessage()
        {
            // Arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply { Message = $"Hello {r.Name}" });
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();
            httpContext.Request.RouteValues["name"] = "TestName!";
            httpContext.Request.RouteValues["sub_message.sub_field"] = "Subfield!";

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.NotNull(request);
            Assert.Equal("TestName!", request!.Name);
            Assert.Equal("Subfield!", request!.SubMessage.SubField);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);
            Assert.Equal("Hello TestName!", responseJson.RootElement.GetProperty("message").GetString());
        }

        [Fact]
        public async Task HandleCallAsync_MatchingRouteValueWithJsonName_SetOnRequestMessage()
        {
            // Arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply { Message = $"Hello {r.Name}" });
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();
            httpContext.Request.RouteValues["name"] = "TestName!";
            httpContext.Request.RouteValues["subMessage.subField"] = "Subfield!";

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.NotNull(request);
            Assert.Equal("TestName!", request!.Name);
            Assert.Equal("Subfield!", request!.SubMessage.SubField);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);
            Assert.Equal("Hello TestName!", responseJson.RootElement.GetProperty("message").GetString());
        }

        [Fact]
        public async Task HandleCallAsync_MatchingQueryStringValues_SetOnRequestMessage()
        {
            // Arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["name"] = "TestName!",
                ["subMessage.subField"] = "TestSubfield!"
            });

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.NotNull(request);
            Assert.Equal("TestName!", request!.Name);
            Assert.Equal("TestSubfield!", request!.SubMessage.SubField);
        }

        [Theory]
        [InlineData("post", "application/x-www-form-urlencoded")]
        [InlineData("put", "application/x-www-form-urlencoded")]
        [InlineData("patch", "application/x-www-form-urlencoded")]
        [InlineData("delete", "application/x-www-form-urlencoded")]
        [InlineData("post", "multipart/form-data")]
        [InlineData("put", "multipart/form-data")]
        [InlineData("patch", "multipart/form-data")]
        [InlineData("delete", "multipart/form-data")]
        public async Task HandleCallAsync_MatchingFormValuesWithJsonName_SetOnRequestMessage(string method, string contentType)
        {
            // Arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();
            httpContext.Request.Method = method;
            httpContext.Request.ContentType = contentType;
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>()
            {
                ["name"] = "TestName!",
                ["subMessage.subField"] = "TestSubfield!"
            });

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.NotNull(request);
            Assert.Equal("TestName!", request!.Name);
            Assert.Equal("TestSubfield!", request!.SubMessage.SubField);
        }

        [Theory]
        [InlineData("post", "application/x-www-form-urlencoded")]
        [InlineData("put", "application/x-www-form-urlencoded")]
        [InlineData("patch", "application/x-www-form-urlencoded")]
        [InlineData("delete", "application/x-www-form-urlencoded")]
        [InlineData("post", "multipart/form-data")]
        [InlineData("put", "multipart/form-data")]
        [InlineData("patch", "multipart/form-data")]
        [InlineData("delete", "multipart/form-data")]
        public async Task HandleCallAsync_MatchingFormValues_SetOnRequestMessage(string method, string contentType)
        {
            // Arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();
            httpContext.Request.Method = method;
            httpContext.Request.ContentType = contentType;
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>()
            {
                ["name"] = "TestName!",
                ["sub_message.sub_field"] = "TestSubfield!"
            });

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.NotNull(request);
            Assert.Equal("TestName!", request!.Name);
            Assert.Equal("TestSubfield!", request!.SubMessage.SubField);
        }

        [Fact]
        public async Task HandleCallAsync_SuccessfulResponse_WithoutDefaultValuesInResponseJson()
        {
            // Arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["name"] = "TestName!"
            });

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.NotNull(request);
            Assert.Equal("TestName!", request!.Name);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(httpContext.Response.Body);
            var responseJson = reader.ReadToEnd();

            Assert.Equal("{ }", responseJson);
        }

        [Theory]
        [InlineData("{malformed_json}", "Request JSON payload is not correctly formatted.")]
        [InlineData("{\"name\": 1234}", "Unsupported conversion from JSON number for field type String")]
        [InlineData("{\"abcd\": 1234}", "Unknown field: abcd")]
        public async Task HandleCallAsync_MalformedRequestBody_BadRequestReturned(string json, string expectedError)
        {
            // Arrange
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();
            httpContext.Request.Method = "post";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            httpContext.Request.ContentType = "application/json";
            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.Equal(400, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);
            Assert.Equal(expectedError, responseJson.RootElement.GetProperty("message").GetString());
            Assert.Equal(expectedError, responseJson.RootElement.GetProperty("error").GetString());
            Assert.Equal((int)StatusCode.InvalidArgument, responseJson.RootElement.GetProperty("code").GetInt32());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("text/html")]
        public async Task HandleCallAsync_BadContentType_BadRequestReturned(string contentType)
        {
            // Arrange
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();
            httpContext.Request.Method = "post";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
            httpContext.Request.ContentType = contentType;
            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.Equal(400, httpContext.Response.StatusCode);

            var expectedError = "Request content-type is invalid.";
            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);
            Assert.Equal(expectedError, responseJson.RootElement.GetProperty("message").GetString());
            Assert.Equal(expectedError, responseJson.RootElement.GetProperty("error").GetString());
            Assert.Equal((int)StatusCode.InvalidArgument, responseJson.RootElement.GetProperty("code").GetInt32());
        }

        [Fact]
        public async Task HandleCallAsync_RpcExceptionReturned_StatusReturned()
        {
            // Arrange
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                return Task.FromException<HelloReply>(new RpcException(new Status(StatusCode.Unauthenticated, "Detail!"), "Message!"));
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.Equal(401, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);
            Assert.Equal("Message!", responseJson.RootElement.GetProperty("message").GetString());
            Assert.Equal("Message!", responseJson.RootElement.GetProperty("error").GetString());
            Assert.Equal((int)StatusCode.Unauthenticated, responseJson.RootElement.GetProperty("code").GetInt32());
        }

        [Fact]
        public async Task HandleCallAsync_RpcExceptionThrown_StatusReturned()
        {
            // Arrange
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Detail!"), "Message!");
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.Equal(401, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);
            Assert.Equal("Message!", responseJson.RootElement.GetProperty("message").GetString());
            Assert.Equal("Message!", responseJson.RootElement.GetProperty("error").GetString());
            Assert.Equal((int)StatusCode.Unauthenticated, responseJson.RootElement.GetProperty("code").GetInt32());
        }

        [Fact]
        public async Task HandleCallAsync_StatusSet_StatusReturned()
        {
            // Arrange
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                c.Status = new Status(StatusCode.Unauthenticated, "Detail!");
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.Equal(401, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);
            Assert.Equal(@"Status(StatusCode=""Unauthenticated"", Detail=""Detail!"")", responseJson.RootElement.GetProperty("message").GetString());
            Assert.Equal(@"Status(StatusCode=""Unauthenticated"", Detail=""Detail!"")", responseJson.RootElement.GetProperty("error").GetString());
            Assert.Equal((int)StatusCode.Unauthenticated, responseJson.RootElement.GetProperty("code").GetInt32());
        }

        [Fact]
        public async Task HandleCallAsync_UserState_HttpContextInUserState()
        {
            object requestHttpContext = null;

            // Arrange
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                c.UserState.TryGetValue("__HttpContext", out requestHttpContext);
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.Equal(httpContext, requestHttpContext);
        }

        [Fact]
        public async Task HandleCallAsync_HasInterceptor_InterceptorCalled()
        {
            object interceptorRun = null;

            // Arrange
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                c.UserState.TryGetValue("IntercepterRun", out interceptorRun);
                return Task.FromResult(new HelloReply());
            };

            var interceptors = new List<(Type Type, object[] Args)>();
            interceptors.Add((typeof(TestInterceptor), Args: Array.Empty<object>()));

            var unaryServerCallHandler = CreateCallHandler(invoker, interceptors: interceptors);
            var httpContext = CreateHttpContext();

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.True((bool)interceptorRun!);
        }

        public class TestInterceptor : Interceptor
        {
            public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
            {
                context.UserState["IntercepterRun"] = true;
                return base.UnaryServerHandler(request, context, continuation);
            }
        }

        [Fact]
        public async Task HandleCallAsync_GetHostAndMethodAndPeer_MatchHandler()
        {
            string peer = null;
            string host = null;
            string method = null;

            // Arrange
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                peer = c.Peer;
                host = c.Host;
                method = c.Method;
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.Equal("ipv4:127.0.0.1:0", peer);
            Assert.Equal("localhost", host);
            Assert.Equal("/ServiceName/TestMethodName", method);
        }

        [Fact]
        public async Task HandleCallAsync_ExceptionThrown_StatusReturned()
        {
            // Arrange
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                throw new InvalidOperationException("Exception!");
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.Equal(500, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);
            Assert.Equal("Exception was thrown by handler.", responseJson.RootElement.GetProperty("message").GetString());
            Assert.Equal("Exception was thrown by handler.", responseJson.RootElement.GetProperty("error").GetString());
            Assert.Equal((int)StatusCode.Unknown, responseJson.RootElement.GetProperty("code").GetInt32());
        }

        [Fact]
        public async Task HandleCallAsync_MatchingRepeatedQueryStringValuesWithJsonName_SetOnRequestMessage()
        {
            // Arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["subMessage.subFields"] = new StringValues(new[] { "TestSubfields1!", "TestSubfields2!" })
            });

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.NotNull(request);
            Assert.Equal(2, request!.SubMessage.SubFields.Count);
            Assert.Equal("TestSubfields1!", request!.SubMessage.SubFields[0]);
            Assert.Equal("TestSubfields2!", request!.SubMessage.SubFields[1]);
        }

        [Fact]
        public async Task HandleCallAsync_MatchingRepeatedQueryStringValues_SetOnRequestMessage()
        {
            // Arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["sub_message.sub_fields"] = new StringValues(new[] { "TestSubfields1!", "TestSubfields2!" })
            });

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.NotNull(request);
            Assert.Equal(2, request!.SubMessage.SubFields.Count);
            Assert.Equal("TestSubfields1!", request!.SubMessage.SubFields[0]);
            Assert.Equal("TestSubfields2!", request!.SubMessage.SubFields[1]);
        }

        [Fact]
        public async Task HandleCallAsync_DataTypes_SetOnRequestMessage()
        {
            // Arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply());
            };

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext();
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["single_int32"] = "1",
                ["single_int64"] = "2",
                ["single_uint32"] = "3",
                ["single_uint64"] = "4",
                ["single_sint32"] = "5",
                ["single_sint64"] = "6",
                ["single_fixed32"] = "7",
                ["single_fixed64"] = "8",
                ["single_sfixed32"] = "9",
                ["single_sfixed64"] = "10",
                ["single_float"] = "11.1",
                ["single_double"] = "12.1",
                ["single_bool"] = "true",
                ["single_string"] = "A string",
                ["single_bytes"] = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                ["single_enum"] = "FOO",
                ["sub_message.sub_field"] = "Nested string"
            });

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.NotNull(request);
            Assert.Equal(1, request!.SingleInt32);
            Assert.Equal(2, request!.SingleInt64);
            Assert.Equal((uint)3, request!.SingleUint32);
            Assert.Equal((ulong)4, request!.SingleUint64);
            Assert.Equal(5, request!.SingleSint32);
            Assert.Equal(6, request!.SingleSint64);
            Assert.Equal((uint)7, request!.SingleFixed32);
            Assert.Equal((ulong)8, request!.SingleFixed64);
            Assert.Equal(9, request!.SingleSfixed32);
            Assert.Equal(10, request!.SingleSfixed64);
            Assert.Equal(11.1, request!.SingleFloat, 3);
            Assert.Equal(12.1, request!.SingleDouble, 3);
            Assert.True(request!.SingleBool);
            Assert.Equal("A string", request!.SingleString);
            Assert.Equal(new byte[] { 1, 2, 3 }, request!.SingleBytes.ToByteArray());
            Assert.Equal("Nested string", request!.SubMessage.SubField);
        }

        [Fact]
        public async Task HandleCallAsync_ParseRequest_UseHttpRequestParsePlugin()
        {
            // arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply());
            };

            var parsePlugin = new Mock<IHttpRequestParsePlugin>();
            parsePlugin.Setup(x => x.ParseAsync(It.IsAny<HttpRequest>())).Returns(
                () =>
                Task.FromResult(
                    (
                        (IMessage)new HelloRequest()
                        {
                            Name = "TestName2!",
                            SubMessage = new SubMessage()
                            {
                                SubField = "TestSubfield2!"
                            }
                        }
                        , StatusCode.OK,
                        "")
                    )
            );

            var httpApiOptions = new HttpApiOption()
            {
                Plugin = typeof(IHttpRequestParsePlugin).AssemblyQualifiedName
            };

            var unaryServerCallHandler = CreateCallHandler(invoker, httpApiOptions: httpApiOptions);
            var httpContext = CreateHttpContext((services) => services.AddScoped<IHttpRequestParsePlugin>((_) => parsePlugin.Object));
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["name"] = "TestName!",
                ["subMessage.subField"] = "TestSubfield!"
            });

            // act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // assert
            request.Should().NotBeNull();
            request!.Name.Should().Equals("TestName2!");
            request!.SubMessage.SubField.Should().Equals("TestSubfield2!");
        }

        [Fact]
        public async Task HandleCallAsync_ReturnUnsuccessfulStatus_UseHttpRequestParsePlugin()
        {
            // arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply());
            };
            var parsePlugin = new Mock<IHttpRequestParsePlugin>();
            parsePlugin.Setup(x => x.ParseAsync(It.IsAny<HttpRequest>())).Returns(
                () =>
                Task.FromResult(
                    (
                        default(IMessage)
                        , StatusCode.Internal,
                        "Internal Error.")
                    )
            );
            var httpApiOptions = new HttpApiOption()
            {
                Plugin = typeof(IHttpRequestParsePlugin).AssemblyQualifiedName
            };

            var unaryServerCallHandler = CreateCallHandler(invoker, httpApiOptions: httpApiOptions);
            var httpContext = CreateHttpContext((services) => services.AddScoped<IHttpRequestParsePlugin>((_) => parsePlugin.Object));
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["name"] = "TestName!",
                ["subMessage.subField"] = "TestSubfield!"
            });

            // act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // assert
            Assert.Equal(500, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);
            var message = responseJson.RootElement.GetProperty("message").GetString();
            var error = responseJson.RootElement.GetProperty("error").GetString();
            var code = responseJson.RootElement.GetProperty("code").GetInt32();

            message.Should().Equals("Internal Error.");
            error.Should().Equals("Internal Error.");
            code.Should().Equals((int)StatusCode.Internal);
        }

        [Fact]
        public async Task HandleCallAsync_ParseRequest_UseHttpRequestParsePostPlugin()
        {
            // arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply());
            };

            var parsePlugin = new Mock<IHttpRequestParsePostPlugin>();
            parsePlugin.Setup(x => x.ParseAsync(It.IsAny<HttpRequest>(), It.IsAny<IMessage>())).Returns(
                (HttpRequest _, IMessage message) =>
                {
                    if (message is HelloRequest reqMessage)
                    {
                        reqMessage.Name += " With UseHttpRequestParsePostPlugin";
                        reqMessage.SubMessage.SubField += " With UseHttpRequestParsePostPlugin";
                    }

                    return Task.FromResult((StatusCode.OK, ""));
                }
             );

            var httpApiOptions = new HttpApiOption()
            {
                Plugin = typeof(IHttpRequestParsePostPlugin).AssemblyQualifiedName
            };

            var unaryServerCallHandler = CreateCallHandler(invoker, httpApiOptions: httpApiOptions);
            var httpContext = CreateHttpContext((services) => services.AddScoped<IHttpRequestParsePostPlugin>((_) => parsePlugin.Object));
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["name"] = "TestName!",
                ["subMessage.subField"] = "TestSubfield!"
            });

            // act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // assert
            request.Should().NotBeNull();
            request!.Name.Should().Equals("TestName! With UseHttpRequestParsePostPlugin");
            request!.SubMessage.SubField.Should().Equals("TestSubfield2! With UseHttpRequestParsePostPlugin");
        }

        [Fact]
        public async Task HandleCallAsync_ReturnUnsuccessfulStatus_UseHttpRequestParsePostPlugin()
        {
            // arrange
            HelloRequest request = null;
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                request = r;
                return Task.FromResult(new HelloReply());
            };
            var parsePlugin = new Mock<IHttpRequestParsePostPlugin>();
            parsePlugin.Setup(x => x.ParseAsync(It.IsAny<HttpRequest>(), It.IsAny<IMessage>())).Returns(
                () =>
                Task.FromResult(
                        (
                            StatusCode.Internal,
                            "Internal Error."
                        )
                    )
            );
            var httpApiOptions = new HttpApiOption()
            {
                Plugin = typeof(IHttpRequestParsePostPlugin).AssemblyQualifiedName
            };

            var unaryServerCallHandler = CreateCallHandler(invoker, httpApiOptions: httpApiOptions);
            var httpContext = CreateHttpContext((services) => services.AddScoped<IHttpRequestParsePostPlugin>((_) => parsePlugin.Object));
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["name"] = "TestName!",
                ["subMessage.subField"] = "TestSubfield!"
            });

            // act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // assert
            Assert.Equal(500, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);
            var message = responseJson.RootElement.GetProperty("message").GetString();
            var error = responseJson.RootElement.GetProperty("error").GetString();
            var code = responseJson.RootElement.GetProperty("code").GetInt32();

            message.Should().Equals("Internal Error.");
            error.Should().Equals("Internal Error.");
            code.Should().Equals((int)StatusCode.Internal);
        }



        [Fact]
        public async Task HandleCallAsync_RedactResponseMessage_UseHttpOutputProcessPlugin()
        {
            // arrange          
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {               
                return Task.FromResult(new HelloReply() {  Message ="Test Message!"});
            };

            var parsePlugin = new Mock<IHttpOutputProcessPlugin>();
            parsePlugin.Setup(x => x.ProcessAsync(It.IsAny<HttpResponse>(), It.IsAny<Encoding>() ,It.IsAny<object>())).Returns(
                (HttpResponse _, Encoding __, object message) =>
                {
                    if (message is HelloReply replyMessage)
                    {
                        replyMessage.Message += " Use HttpOutputProcessPlugin";
                    }

                    return Task.FromResult(false);
                }
             );

            var httpApiOptions = new HttpApiOption()
            {
                Plugin = typeof(IHttpOutputProcessPlugin).AssemblyQualifiedName
            };

            var unaryServerCallHandler = CreateCallHandler(invoker, httpApiOptions: httpApiOptions);
            var httpContext = CreateHttpContext((services) => services.AddScoped<IHttpOutputProcessPlugin>((_) => parsePlugin.Object));
          

            // act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // assert
            Assert.Equal(200, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);
            var message = responseJson.RootElement.GetProperty("message").GetString();
            message.Should().Equals("Test Message!  Use HttpOutputProcessPlugin");
            
        }

        [Fact]
        public async Task HandleCallAsync_ReplaceDefaultResponse_UseHttpOutputProcessPlugin()
        {
            // arrange          
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                return Task.FromResult(new HelloReply() { Message = "Test Message!" });
            };

            var parsePlugin = new Mock<IHttpOutputProcessPlugin>();
            parsePlugin.Setup(x => x.ProcessAsync(It.IsAny<HttpResponse>(), It.IsAny<Encoding>(), It.IsAny<object>())).Returns(
                async (HttpResponse res, Encoding __, object message) =>
                {
                    if (message is HelloReply replyMessage)
                    {
                        replyMessage.Message += " Use HttpOutputProcessPlugin";
                        res.StatusCode = (int)HttpStatusCode.OK;
                        res.ContentType = "text/plain";
                        await res.WriteAsync(replyMessage.Message);
                        return true;
                    }
                    return false;                   
                }
             );

            var httpApiOptions = new HttpApiOption()
            {
                Plugin = typeof(IHttpOutputProcessPlugin).AssemblyQualifiedName
            };

            var unaryServerCallHandler = CreateCallHandler(invoker, httpApiOptions: httpApiOptions);
            var httpContext = CreateHttpContext((services) => services.AddScoped<IHttpOutputProcessPlugin>((_) => parsePlugin.Object));


            // act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // assert
            Assert.Equal(200, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(httpContext.Response.Body);
            var responseBody = reader.ReadToEnd();
            responseBody.Should().Equals("Test Message!");
        }

        [Fact]
        public async Task HandleCallAsync_PostProcessErrorResponse_UseHttpApiErrorProcess()
        {
            // Arrange
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                throw new InvalidOperationException("Exception!");
            };

            var process = new Mock<IHttpApiErrorProcess>();

            process.Setup(x => x.ProcessAsync(It.IsAny<HttpResponse>(), It.IsAny<Encoding>(), It.IsAny<Error>())).Returns(
                (HttpResponse _, Encoding __, Error error) =>
                {
                    error.Message = "Internal Error.";                   
                    return Task.FromResult(false);
                }
                );
            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext((services) => services.AddScoped<IHttpApiErrorProcess>(_ => process.Object));

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.Equal(500, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);           
            using var responseJson = JsonDocument.Parse(httpContext.Response.Body);

            var message = responseJson.RootElement.GetProperty("message").GetString();
            var error = responseJson.RootElement.GetProperty("error").GetString();
            var code = responseJson.RootElement.GetProperty("code").GetInt32();


            message.Should().Equals("Exception was thrown by handler.");
            error.Should().Equals("Internal Error.");
            code.Should().Equals((int)StatusCode.Unknown);
          
        }

        [Fact]
        public async Task HandleCallAsync_HandlerErrorResponse_UseHttpApiErrorProcess()
        {
            // Arrange
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                throw new InvalidOperationException("Exception!");
            };

            var process = new Mock<IHttpApiErrorProcess>();

            process.Setup(x => x.ProcessAsync(It.IsAny<HttpResponse>(), It.IsAny<Encoding>(), It.IsAny<Error>())).Returns(
                async (HttpResponse res,Encoding _, Error error) =>
                {
                    res.StatusCode = (int)HttpStatusCode.InternalServerError;
                    res.ContentType = "text/plain";
                    await res.WriteAsync(error.Message);
                    return true;
                }
                );
            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext(( services)=>  services.AddScoped<IHttpApiErrorProcess>(_=> process.Object) );

            // Act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // Assert
            Assert.Equal(500, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(httpContext.Response.Body);
            var responseBody = reader.ReadToEnd();
            responseBody.Should().Equals("Exception was thrown by handler.");        
        }





        [Fact]
        public async Task HandleCallAsync_RedactResponseMessage_UseHttpApiOutputProcess()
        {
            // arrange          
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                var reply = new HelloReply() { Message = "Test Message!" };
                reply.ListMessage.Add("message 1");
                reply.ListMessage.Add("message 2");
                reply.ListMessage.Add("message 3");
                return Task.FromResult(reply);
            };

            var parsePlugin = new Mock<IHttpApiOutputProcess>();
            parsePlugin.Setup(x => x.ProcessAsync(It.IsAny<HttpResponse>(), It.IsAny<Encoding>(), It.IsAny<object>())).Returns(
                (HttpResponse _, Encoding __, object message) =>
                {
                    if (message is HelloReply replyMessage)
                    {
                        return Task.FromResult((false, (object)replyMessage.ListMessage));
                    }
                    return Task.FromResult((false, message));

                }
             );          

            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext((services) => services.AddScoped<IHttpApiOutputProcess>((_) => parsePlugin.Object));


            // act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // assert
            Assert.Equal(200, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(httpContext.Response.Body);
            var responseBody = reader.ReadToEnd();

            responseBody.Should().Equals("[ \"message 1\", \"message 2\", \"message 3\" ]");

        }

        [Fact]
        public async Task HandleCallAsync_ReplaceDefaultResponse_UseHttpApiOutputProcess()
        {
            // arrange          
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, c) =>
            {
                return Task.FromResult(new HelloReply() { Message = "Test Message!" });
            };

            var parsePlugin = new Mock<IHttpApiOutputProcess>();
            parsePlugin.Setup(x => x.ProcessAsync(It.IsAny<HttpResponse>(), It.IsAny<Encoding>(), It.IsAny<object>())).Returns(
                async (HttpResponse res, Encoding __, object message) =>
                {
                    if (message is HelloReply replyMessage)
                    {
                        replyMessage.Message += " Use UseHttpApiOutputProcess";
                        res.StatusCode = (int)HttpStatusCode.OK;
                        res.ContentType = "text/plain";
                        await res.WriteAsync(replyMessage.Message);
                        return (true, message);
                    }
                    return (false, message);
                }
             );

         
            var unaryServerCallHandler = CreateCallHandler(invoker);
            var httpContext = CreateHttpContext((services) => services.AddScoped<IHttpApiOutputProcess>((_) => parsePlugin.Object));


            // act
            await unaryServerCallHandler.HandleCallAsync(httpContext);

            // assert
            Assert.Equal(200, httpContext.Response.StatusCode);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(httpContext.Response.Body);
            var responseBody = reader.ReadToEnd();
            responseBody.Should().Equals("Test Message! Use UseHttpApiOutputProcess");
        }





















        private static DefaultHttpContext CreateHttpContext(Action<ServiceCollection> additionalServices = null, CancellationToken cancellationToken = default)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<GrpcHttpApiGreeterService>();
            serviceCollection.AddSingleton(typeof(IGrpcInterceptorActivator<>), typeof(TestInterceptorActivator<>));
            additionalServices?.Invoke(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString("localhost");
            httpContext.RequestServices = serviceProvider;
            httpContext.Response.Body = new MemoryStream();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            httpContext.Features.Set<IHttpRequestLifetimeFeature>(new HttpRequestLifetimeFeature(cancellationToken));
            return httpContext;
        }

        private class TestInterceptorActivator<T> : IGrpcInterceptorActivator<T> where T : Interceptor
        {
            public GrpcActivatorHandle<Interceptor> Create(IServiceProvider serviceProvider, InterceptorRegistration interceptorRegistration)
            {
                return new GrpcActivatorHandle<Interceptor>(Activator.CreateInstance<T>(), created: true, state: null);
            }

            public ValueTask ReleaseAsync(GrpcActivatorHandle<Interceptor> interceptor)
            {
                return default;
            }
        }

        private class HttpRequestLifetimeFeature : IHttpRequestLifetimeFeature
        {
            public HttpRequestLifetimeFeature(CancellationToken cancellationToken)
            {
                RequestAborted = cancellationToken;
            }

            public CancellationToken RequestAborted { get; set; }

            public void Abort()
            {
            }
        }

        private static UnaryServerCallHandler<GrpcHttpApiGreeterService, HelloRequest, HelloReply> CreateCallHandler(
            UnaryServerMethod<GrpcHttpApiGreeterService, HelloRequest, HelloReply> invoker,
            List<(Type Type, object[] Args)> interceptors = null,
            HttpApiOption httpApiOptions = null,
            GrpcHttpApiOptions grpcHttpApiOptions = null)
        {
            var serviceOptions = new GrpcServiceOptions();
            if (interceptors != null)
            {
                foreach (var interceptor in interceptors)
                {
                    serviceOptions.Interceptors.Add(interceptor.Type, interceptor.Args ?? Array.Empty<object>());
                }
            }

            var unaryServerCallInvoker = new UnaryServerMethodInvoker<GrpcHttpApiGreeterService, HelloRequest, HelloReply>(
                invoker,
                CreateServiceMethod<HelloRequest, HelloReply>("TestMethodName", HelloRequest.Parser, HelloReply.Parser),
                Internal.MethodOptions.Create(new[] { serviceOptions }),
                new TestGrpcServiceActivator<GrpcHttpApiGreeterService>());

            return new UnaryServerCallHandler<GrpcHttpApiGreeterService, HelloRequest, HelloReply>(
                unaryServerCallInvoker,
                httpApiOptions ?? new HttpApiOption(),
                grpcHttpApiOptions ?? new GrpcHttpApiOptions());
        }

        public static Marshaller<TMessage> GetMarshaller<TMessage>(MessageParser<TMessage> parser) where TMessage : IMessage<TMessage> =>
            Marshallers.Create<TMessage>(r => r.ToByteArray(), data => parser.ParseFrom(data));

        public static readonly Method<HelloRequest, HelloReply> ServiceMethod = CreateServiceMethod("MethodName", HelloRequest.Parser, HelloReply.Parser);

        public static Method<TRequest, TResponse> CreateServiceMethod<TRequest, TResponse>(string methodName, MessageParser<TRequest> requestParser, MessageParser<TResponse> responseParser)
             where TRequest : IMessage<TRequest>
             where TResponse : IMessage<TResponse>
        {
            return new Method<TRequest, TResponse>(MethodType.Unary, "ServiceName", methodName, GetMarshaller(requestParser), GetMarshaller(responseParser));
        }
    }
}