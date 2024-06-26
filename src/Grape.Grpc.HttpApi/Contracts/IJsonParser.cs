﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Grape.Grpc.HttpApi.Contracts
{
    public interface IJsonParser
    {
        string ToJson(object item);

        string ToJson<T>(T item) where T : class;

        object FromJson(string json, Type type);

        T FromJson<T>(string json) where T : class;
    }
}
