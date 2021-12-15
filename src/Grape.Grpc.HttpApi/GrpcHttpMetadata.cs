using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Grape.Grpc.HttpApi
{
    /// <summary>
    /// Metadata for a gRPC HTTP API endpoint.
    /// </summary>
    public class GrpcHttpMetadata
    {
        /// <summary>
        /// Creates a new instance of <see cref="GrpcHttpMetadata"/> with the provided Protobuf
        /// <see cref="Google.Protobuf.Reflection.MethodDescriptor"/> and <see cref="Google.Api.HttpRule"/>.
        /// </summary>
        /// <param name="handerMethod">method to handler the request.</param>
        /// <param name="methodDescriptor">The Protobuf <see cref="Google.Protobuf.Reflection.MethodDescriptor"/>.</param>
        /// <param name="httpApiOption">The <see cref="Grape.Grpc.HttpApi.HttpApiOption"/>.</param>
        public GrpcHttpMetadata(MethodInfo handerMethod, MethodDescriptor methodDescriptor, HttpApiOption httpApiOption)
        {
            HanderMethod = handerMethod;
            MethodDescriptor = methodDescriptor;
            HttpApiOption = httpApiOption;
        }

        public Type HanderServiceType { 
            get {
                return this.HanderMethod?.DeclaringType;
            } 
        }

        public MethodInfo HanderMethod{ get; }

        /// <summary>
        /// Gets the Protobuf <see cref="Google.Protobuf.Reflection.MethodDescriptor"/>.
        /// </summary>
        public MethodDescriptor MethodDescriptor { get; }

        /// <summary>
        /// Gets the <see cref="Grape.Grpc.HttpApi.HttpApiOption"/>.
        /// </summary>
        public HttpApiOption HttpApiOption { get; }
    }
}
