﻿using System.Text.Json.Serialization;

namespace McpDotNet.Protocol.Types;

/// <summary>
/// Represents the content of a resource.
/// </summary>
public class ResourceContents
{
    /// <summary>
    /// The URI of the resource.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// The type of content.
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    /// <summary>
    /// The text content of the resource.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }


    /// <summary>
    /// The base64-encoded binary content of the resource.
    /// </summary>
    [JsonPropertyName("blob")]
    public string? Blob { get; set; }
}