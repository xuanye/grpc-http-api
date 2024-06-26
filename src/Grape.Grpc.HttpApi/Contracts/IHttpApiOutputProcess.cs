﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Grape.Grpc.HttpApi.Contracts
{
    public interface IHttpApiOutputProcess
    {
        Task<(bool processed, object newResponseBody)> ProcessAsync(HttpResponse response, Encoding encoding, object responseBody);
    }
}
