using Google.Protobuf;
using Grape.Grpc.HttpApi.Contracts;
using Grape.Grpc.HttpApi.Implements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grape.Grpc.HttpApi
{
    public class GrpcHttpApiOptions
    {
        private static readonly JsonFormatter DefaultFormatter = new JsonFormatter(new JsonFormatter.Settings(false).WithFormatEnumsAsIntegers(true));//.WithFormatDefaultValues(true)

        private static readonly IJsonParser DefaultJsonParser = new DefaultJsonParser(DefaultFormatter);
        /// <summary>
        /// Gets or sets the <see cref="Google.Protobuf.JsonFormatter"/> used to serialize outgoing messages.
        /// </summary>
        public JsonFormatter JsonFormatter { get; set; } = DefaultFormatter;

        public IJsonParser JsonParser { get; set; } = DefaultJsonParser;
    }
}
