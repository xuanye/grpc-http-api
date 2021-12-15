using System.Runtime.Serialization;

namespace Grape.Grpc.HttpApi.Swagger
{
    [DataContract]
    public class SwaggerApiContactInfo
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name = "email")]
        public string Email { get; set; }
    }
}
