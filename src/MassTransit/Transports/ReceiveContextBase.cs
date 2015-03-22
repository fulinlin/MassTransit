// Copyright 2007-2015 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Transports
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Serialization;


    public abstract class ReceiveContextBase :
        ReceiveContext
    {
        static readonly ContentType DefaultContentType = JsonMessageSerializer.JsonContentType;

        readonly CancellationTokenSource _cancellationTokenSource;
        readonly Lazy<ContentType> _contentType;
        readonly Lazy<ContextHeaders> _headers;
        readonly Uri _inputAddress;
        readonly PayloadCache _payloadCache;
        readonly Stopwatch _receiveTimer;
        readonly bool _redelivered;

        protected ReceiveContextBase(Uri inputAddress, bool redelivered)
        {
            _receiveTimer = Stopwatch.StartNew();

            _payloadCache = new PayloadCache();

            _inputAddress = inputAddress;
            _redelivered = redelivered;

            _cancellationTokenSource = new CancellationTokenSource();

            _headers = new Lazy<ContextHeaders>(() => new JsonContextHeaders(HeaderProvider));

            _contentType = new Lazy<ContentType>(GetContentType);
        }

        protected abstract IContextHeaderProvider HeaderProvider { get; }

        bool PipeContext.HasPayloadType(Type contextType)
        {
            return _payloadCache.HasPayloadType(contextType);
        }

        bool PipeContext.TryGetPayload<TPayload>(out TPayload context)
        {
            return _payloadCache.TryGetPayload(out context);
        }

        TPayload PipeContext.GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        {
            return _payloadCache.GetOrAddPayload(payloadFactory);
        }

        CancellationToken PipeContext.CancellationToken
        {
            get { return _cancellationTokenSource.Token; }
        }

        bool ReceiveContext.Redelivered
        {
            get { return _redelivered; }
        }

        ContextHeaders ReceiveContext.TransportHeaders
        {
            get { return _headers.Value; }
        }

        void ReceiveContext.NotifyConsumed(TimeSpan elapsed, string messageType, string consumerType)
        {
        }

        async Task ReceiveContext.NotifyFaulted<T>(T message, string consumerType, Exception exception)
        {
        }

        Stream ReceiveContext.Body
        {
            get { return GetBodyStream(); }
        }

        TimeSpan ReceiveContext.ElapsedTime
        {
            get { return _receiveTimer.Elapsed; }
        }

        Uri ReceiveContext.InputAddress
        {
            get { return _inputAddress; }
        }

        ContentType ReceiveContext.ContentType
        {
            get { return _contentType.Value; }
        }

        protected abstract Stream GetBodyStream();

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        protected virtual ContentType GetContentType()
        {
            object contentTypeHeader;
            if (_headers.Value.TryGetHeader("Content-Type", out contentTypeHeader))
            {
                var contentType = contentTypeHeader as ContentType;
                if (contentType != null)
                    return contentType;

                var contentTypeString = contentTypeHeader as string;
                if (contentTypeString != null)
                    return new ContentType(contentTypeString);
            }

            return DefaultContentType;
        }
    }
}