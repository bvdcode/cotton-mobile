// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public static partial class CottonRefreshTokenRejection
    {
        public static bool IsConfirmed(CottonTokenRefreshException refreshFailure, ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(refreshFailure);
            if (refreshFailure.InnerException is not CottonApiException exception
                || exception.StatusCode != HttpStatusCode.NotFound
                || string.IsNullOrWhiteSpace(exception.ResponseBody))
            {
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(exception.ResponseBody);
                JsonElement problem = document.RootElement;
                return problem.ValueKind == JsonValueKind.Object
                    && problem.TryGetProperty("status", out JsonElement status)
                    && status.ValueKind == JsonValueKind.Number
                    && status.TryGetInt32(out int statusCode)
                    && statusCode == (int)HttpStatusCode.NotFound
                    && problem.TryGetProperty("code", out JsonElement code)
                    && code.ValueKind == JsonValueKind.String
                    && code.GetString() == "not_found"
                    && problem.TryGetProperty("instance", out JsonElement instance)
                    && instance.ValueKind == JsonValueKind.String
                    && instance.GetString() == Routes.V1.Auth + "/refresh";
            }
            catch (JsonException exceptionBody)
            {
                UnrecognizedBody(logger ?? NullLogger.Instance, exceptionBody);
                return false;
            }
        }

        [LoggerMessage(Level = LogLevel.Debug, Message = "Token renewal returned an unrecognized error body.")]
        private static partial void UnrecognizedBody(ILogger logger, Exception exception);
    }
}
