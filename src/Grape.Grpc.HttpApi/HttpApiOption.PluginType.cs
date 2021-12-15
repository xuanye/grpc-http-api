using System;
using System.Collections.Concurrent;

namespace Grape.Grpc.HttpApi
{
    public sealed partial class HttpApiOption
    {
        private static readonly ConcurrentDictionary<string, Type> TypeCache = new ConcurrentDictionary<string, Type>();

        private Type _pluginType;
        public Type PluginType
        {
            get
            {
                if(_pluginType != null)
                {
                    return _pluginType;
                }

                if (!string.IsNullOrEmpty(this.Plugin))
                {
                    if(TypeCache.TryGetValue(this.Plugin,out var type))
                    {
                        _pluginType = type;
                    }
                    else
                    {
                        _pluginType = Type.GetType(this.Plugin);
                        TypeCache.TryAdd(this.Plugin, _pluginType);
                    }
                }
                return _pluginType;
            }          
        }

       
        
    }
}
