using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grpc.HttpApi
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
        /// <param name="methodDescriptor">The Protobuf <see cref="Google.Protobuf.Reflection.MethodDescriptor"/>.</param>
        /// <param name="httpApiOption">The <see cref="Grpc.HttpApi.HttpApiOption"/>.</param>
        public GrpcHttpMetadata(MethodDescriptor methodDescriptor, HttpApiOption httpApiOption)
        {
            MethodDescriptor = methodDescriptor;
            HttpApiOption = httpApiOption;
        }

        /// <summary>
        /// Gets the Protobuf <see cref="Google.Protobuf.Reflection.MethodDescriptor"/>.
        /// </summary>
        public MethodDescriptor MethodDescriptor { get; }

        /// <summary>
        /// Gets the <see cref="Grpc.HttpApi.HttpApiOption"/>.
        /// </summary>
        public HttpApiOption HttpApiOption { get; }
    }
}
