﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// https://raw.githubusercontent.com/aspnet/Hosting/rel/1.1.2/src/Microsoft.AspNetCore.TestHost/RequestFeature.cs


namespace Papercut.Service.Web.Hosting.InProcess;

internal class RequestFeature : IHttpRequestFeature
{
    public RequestFeature()
    {
            Body = Stream.Null;
            Headers = new HeaderDictionary();
            Method = "GET";
            Path = "";
            PathBase = "";
            Protocol = "HTTP/1.1";
            QueryString = "";
            Scheme = "http";
        }

    public Stream Body { get; set; }

    public IHeaderDictionary Headers { get; set; }

    public string Method { get; set; }

    public string Path { get; set; }

    public string PathBase { get; set; }

    public string Protocol { get; set; }

    public string QueryString { get; set; }

    public string Scheme { get; set; }

    public string RawTarget { get; set; }
}