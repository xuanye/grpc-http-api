using Grpc.AspNetCore.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Grape.Grpc.HttpApi.UnitTests.TestObjects
{
    internal class TestGrpcServiceActivator<TGrpcService> : IGrpcServiceActivator<TGrpcService> where TGrpcService : class, new()
    {
        public GrpcActivatorHandle<TGrpcService> Create(IServiceProvider serviceProvider)
        {
            return new GrpcActivatorHandle<TGrpcService>(new TGrpcService(), false, null);
        }

        public ValueTask ReleaseAsync(GrpcActivatorHandle<TGrpcService> service)
        {
            return default;
        }
    }
}
