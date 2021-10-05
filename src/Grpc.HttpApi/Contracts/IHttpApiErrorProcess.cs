using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Grpc.HttpApi.Contracts
{
    public interface IHttpApiErrorProcess
    {
        Task<bool> ProcessAsync(HttpResponse response, Encoding encoding, Error e);
    }
}
