using System;
using System.Collections.Generic;
using System.Text;

namespace Grape.Grpc.HttpApi.Swagger
{
    public interface ISwaggerProvider
    {
        SwaggerInfo GetSwaggerInfo();
    }
}
