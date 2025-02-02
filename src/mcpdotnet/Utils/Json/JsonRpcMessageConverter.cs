﻿using System.Text.Json;
using System.Text.Json.Serialization;
using McpDotNet.Protocol.Messages;
using Microsoft.Extensions.Logging;
using McpDotNet.Logging;

namespace McpDotNet.Utils.Json;

/// <summary>
/// JSON converter for IJsonRpcMessage that handles polymorphic deserialization of different message types.
/// </summary>
internal class JsonRpcMessageConverter : JsonConverter<IJsonRpcMessage>
{
    private readonly ILogger<JsonRpcMessageConverter> _logger;

    public JsonRpcMessageConverter(ILogger<JsonRpcMessageConverter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public override IJsonRpcMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            _logger.JsonRpcMessageConverterExpectedStartObjectToken();
            throw new JsonException("Expected StartObject token");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // All JSON-RPC messages must have a jsonrpc property with value "2.0"
        if (!root.TryGetProperty("jsonrpc", out var versionProperty) ||
            versionProperty.GetString() != "2.0")
        {
            _logger.JsonRpcMessageConverterInvalidJsonRpcVersion();
            throw new JsonException("Invalid or missing jsonrpc version");
        }

        // Determine the message type based on the presence of id, method, and error properties
        bool hasId = root.TryGetProperty("id", out _);
        bool hasMethod = root.TryGetProperty("method", out _);
        bool hasError = root.TryGetProperty("error", out _);

        var rawText = root.GetRawText();

        // Messages with an id but no method are responses
        if (hasId && !hasMethod)
        {
            // Messages with an error property are error responses
            if (hasError)
            {
                _logger.JsonRpcMessageConverterDeserializingErrorResponse(rawText);
                return JsonSerializer.Deserialize<JsonRpcError>(rawText, options);
            }
            // Messages with a result property are success responses
            else if (root.TryGetProperty("result", out _))
            {
                _logger.JsonRpcMessageConverterDeserializingResponse(rawText);
                return JsonSerializer.Deserialize<JsonRpcResponse>(rawText, options);
            }
            _logger.JsonRpcMessageConverterResponseMustHaveResultOrError(rawText);
            throw new JsonException("Response must have either result or error");
        }
        // Messages with a method but no id are notifications
        else if (hasMethod && !hasId)
        {
            _logger.JsonRpcMessageConverterDeserializingNotification(rawText);
            return JsonSerializer.Deserialize<JsonRpcNotification>(rawText, options);
        }
        // Messages with both method and id are requests
        else if (hasMethod && hasId)
        {
            _logger.JsonRpcMessageConverterDeserializingRequest(rawText);
            return JsonSerializer.Deserialize<JsonRpcRequest>(rawText, options);
        }

        _logger.JsonRpcMessageConverterInvalidMessageFormat(rawText);
        throw new JsonException("Invalid JSON-RPC message format");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IJsonRpcMessage value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case JsonRpcRequest request:
                JsonSerializer.Serialize(writer, request, options);
                break;
            case JsonRpcNotification notification:
                JsonSerializer.Serialize(writer, notification, options);
                break;
            case JsonRpcResponse response:
                JsonSerializer.Serialize(writer, response, options);
                break;
            case JsonRpcError error:
                JsonSerializer.Serialize(writer, error, options);
                break;
            default:
                _logger.JsonRpcMessageConverterWriteUnknownMessageType(value.GetType().ToString());
                throw new JsonException($"Unknown JSON-RPC message type: {value.GetType()}");
        }
    }
}