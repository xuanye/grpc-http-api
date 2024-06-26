﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;

namespace Grape.Grpc.HttpApi.Internal
{
    internal static class GrpcProtocolConstants
    {
        internal const string TimeoutHeader = "grpc-timeout";
        internal const string MessageEncodingHeader = "grpc-encoding";
        internal const string MessageAcceptEncodingHeader = "grpc-accept-encoding";

        internal static readonly HashSet<string> FilteredHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            MessageEncodingHeader,
            MessageAcceptEncodingHeader,
            TimeoutHeader,
            HeaderNames.ContentType,
            HeaderNames.TE,
            HeaderNames.Host,
            HeaderNames.AcceptEncoding
        };
    }
}