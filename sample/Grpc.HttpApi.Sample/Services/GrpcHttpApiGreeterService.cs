using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grpc.HttpApi.Sample
{
    public class GrpcHttpApiGreeterService : HttpApiGreeterService.HttpApiGreeterServiceBase
    {
        private readonly ILogger<GrpcHttpApiGreeterService> _logger;
        public GrpcHttpApiGreeterService(ILogger<GrpcHttpApiGreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply() { Message = $"Hello {request.Name},From Method Get" });
        }
        public override Task<HelloReply> PostTest(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply() { Message = $"Hello {request.Name},From Method Post" });
        }
    }
}
