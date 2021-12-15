using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Grape.Grpc.HttpApi.Swagger
{
    [DataContract]
    public class SwaggerDefinition
    {
        [DataMember(Name = "type")]
        public  string Type { get; set; }

        [DataMember(Name = "properties")]
        public Dictionary<string,SwaggerPropertyDefinition> Properties { get; set; }

    }
}
