using Google.Protobuf;
using Grpc.HttpApi.Contracts;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace Grpc.HttpApi.Implements
{
    public class DefaultJsonParser : IJsonParser
    {
        static readonly ConcurrentDictionary<Type, MessageParser> PARSER_CACHE = new ConcurrentDictionary<Type, MessageParser>();
        private static readonly Type BaseType = typeof(IMessage);
        private readonly JsonFormatter _jsonFormatter;

        public DefaultJsonParser(JsonFormatter jsonFormatter)
        {
            _jsonFormatter = jsonFormatter;
        }
              
        private static MessageParser FindMessageParser(Type messageType)
        {
            if (BaseType.IsAssignableFrom(messageType) && messageType.IsClass)
            {
                var property = messageType.GetProperty("Parser");
                if (property != null)
                {
                    return (MessageParser)property.GetValue(null);
                }
            }
            throw new InvalidCastException("Message is not a Protobuf Message");
        }

        public string ToJson(object item)
        {

            if (item is IMessage message)
            {
                return _jsonFormatter.Format(message);
            }
            return null;
        }

        public string ToJson<T>(T item) where T : class
        {


            if (item is IMessage message)
            {
                return _jsonFormatter.Format(message);
            }
            return null;
        }

        public object FromJson(string json, Type type)
        {
            if (PARSER_CACHE.TryGetValue(type, out var parser))
                return parser.ParseJson(json);

            parser = FindMessageParser(type);
            PARSER_CACHE.TryAdd(type, parser);
            return parser.ParseJson(json);
        }

        public T FromJson<T>(string json) where T : class
        {
            var type = typeof(T);
            return (T)FromJson(json, type);
        }

    }
}
