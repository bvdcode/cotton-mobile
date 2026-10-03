// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Util;
using System.Net;
using Xamarin.Android.Net;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidNetworkMessageHandler : AndroidMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (WebException exception) when (exception is
            {
                Status: WebExceptionStatus.UnknownError,
                InnerException: Java.IO.IOException and not Javax.Net.Ssl.SSLException
                        and not Java.Net.ProtocolException
            })
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Log.Debug("CottonNetwork", "Android transport request was cancelled.");
                    throw new OperationCanceledException("Network request was cancelled.", exception, cancellationToken);
                }

                Log.Warn("CottonNetwork", "Android transport connection failed.");
                throw new HttpRequestException("Network connection failed.", exception);
            }
        }
    }
}
#endif
