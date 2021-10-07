using System;
using System.Collections.Generic;
using System.Text;

namespace Grpc.HttpApi.Swagger
{
    public interface ISwaggerProvider
    {
        SwaggerInfo GetSwaggerInfo(string host,string basePath);
    }
}
