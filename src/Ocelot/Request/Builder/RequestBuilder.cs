﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Ocelot.Request.Builder
{
    internal sealed class RequestBuilder
    {
        private HttpMethod _method;
        private string _downstreamUrl;
        private QueryString _queryString;
        private Stream _content;
        private string _contentType;
        private IHeaderDictionary _headers;
        private RequestId.RequestId _requestId;
        private IRequestCookieCollection _cookies;
        private readonly string[] _unsupportedHeaders = {"host"};

        public RequestBuilder WithHttpMethod(string httpMethod)
        {
            _method = new HttpMethod(httpMethod);
            return this;
        }

        public RequestBuilder WithDownstreamUrl(string downstreamUrl)
        {
            _downstreamUrl = downstreamUrl;
            return this;
        }

        public RequestBuilder WithQueryString(QueryString queryString)
        {
            _queryString = queryString;
            return this;
        }

        public RequestBuilder WithContent(Stream content)
        {
            _content = content;
            return this;
        }

        public RequestBuilder WithContentType(string contentType)
        {
            _contentType = contentType;
            return this;
        }

        public RequestBuilder WithHeaders(IHeaderDictionary headers)
        {
            _headers = headers;
            return this;
        }

        public RequestBuilder WithRequestId(RequestId.RequestId requestId)
        {
            _requestId = requestId;
            return this;
        }

        public RequestBuilder WithCookies(IRequestCookieCollection cookies)
        {
            _cookies = cookies;
            return this;
        }

        public async Task<Request> Build()
        {
            var uri = CreateUri();

            var httpRequestMessage = new HttpRequestMessage(_method, uri);

            await AddContentToRequest(httpRequestMessage);

            AddContentTypeToRequest(httpRequestMessage);

            AddHeadersToRequest(httpRequestMessage);

            if (ShouldAddRequestId(_requestId, httpRequestMessage.Headers))
            {
                AddRequestIdHeader(_requestId, httpRequestMessage);
            }

            var cookieContainer = CreateCookieContainer(uri);

            return new Request(httpRequestMessage, cookieContainer);
        }

        private Uri CreateUri()
        {
            var uri = new Uri(string.Format("{0}{1}", _downstreamUrl, _queryString.ToUriComponent()));
            return uri;
        }

        private async Task AddContentToRequest(HttpRequestMessage httpRequestMessage)
        {
            if (_content != null)
            {
                httpRequestMessage.Content = new ByteArrayContent(await ToByteArray(_content));
            }
        }

        private void AddContentTypeToRequest(HttpRequestMessage httpRequestMessage)
        {
            if (!string.IsNullOrEmpty(_contentType))
            {
                httpRequestMessage.Content.Headers.Remove("Content-Type");
                httpRequestMessage.Content.Headers.TryAddWithoutValidation("Content-Type", _contentType);
            }
        }

        private void AddHeadersToRequest(HttpRequestMessage httpRequestMessage)
        {
            if (_headers != null)
            {
                _headers.Remove("Content-Type");

                foreach (var header in _headers)
                {
                    //todo get rid of if..
                    if (IsSupportedHeader(header))
                    {
                        httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }
            }
        }

        private bool IsSupportedHeader(KeyValuePair<string, StringValues> header)
        {
            return !_unsupportedHeaders.Contains(header.Key.ToLower());
        }

        private CookieContainer CreateCookieContainer(Uri uri)
        {
            var cookieContainer = new CookieContainer();

            if (_cookies != null)
            {
                foreach (var cookie in _cookies)
                {
                    cookieContainer.Add(uri, new Cookie(cookie.Key, cookie.Value));
                }
            }

            return cookieContainer;
        }

        private void AddRequestIdHeader(RequestId.RequestId requestId, HttpRequestMessage httpRequestMessage)
        {
            httpRequestMessage.Headers.Add(requestId.RequestIdKey, requestId.RequestIdValue);
        }

        private bool RequestIdInHeaders(RequestId.RequestId requestId, HttpRequestHeaders headers)
        {
            IEnumerable<string> value;
            return headers.TryGetValues(requestId.RequestIdKey, out value);
        }

        private bool ShouldAddRequestId(RequestId.RequestId requestId, HttpRequestHeaders headers)
        {
            return !string.IsNullOrEmpty(requestId?.RequestIdKey)
                   && !string.IsNullOrEmpty(requestId.RequestIdValue)
                   && !RequestIdInHeaders(requestId, headers);
        }

        private async Task<byte[]> ToByteArray(Stream stream)
        {
            using (stream)
            {
                using (var memStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memStream);
                    return memStream.ToArray();
                }
            }
        }
    }
}
