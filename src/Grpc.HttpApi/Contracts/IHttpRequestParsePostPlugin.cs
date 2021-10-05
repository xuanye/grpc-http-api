using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Grpc.HttpApi.Contracts
{
    public interface IHttpRequestParsePostPlugin : IHttpPlugin
    {
        Task<(StatusCode requestStatusCode, string errorMessage)> ParseAsync(HttpRequest request, IMessage requestMessage);
    }
}
