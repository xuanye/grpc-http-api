using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Grape.Grpc.HttpApi.Swagger
{

    [DataContract]
    public class SwaggerItemSchema
    {

    }

    [DataContract]
    public class SwaggerSingleItemSchema:SwaggerItemSchema
    {
        //$ref
        [DataMember(Name = "$ref")]
        public string Ref { get; set; }
    }
    [DataContract]
    public class SwaggerArrayItemSchema:SwaggerItemSchema
    {
        [DataMember(Name = "$ref")]
        public string Type { get; } = "array";

        [DataMember(Name = "items")]
        public List<SwaggerItemSchema> Items { get; set; } = new List<SwaggerItemSchema>();
    }
}
