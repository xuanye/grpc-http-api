using System.Runtime.Serialization;

namespace Grape.Grpc.HttpApi.Swagger
{

    [DataContract]
    public class SwaggerApiResponse
    {
        [DataMember(Name = "description")]
        public  string Description { get; set; }

        [DataMember(Name = "schema")]
        public SwaggerItemSchema Schema { get; set; }
    }
}
