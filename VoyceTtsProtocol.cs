using System.Globalization;
using System.Text.Json.Serialization;

namespace STranslate.Plugin.Tts.VoyceTTS;

internal static class VoyceTtsProtocol
{
    internal const string WangwangitUrl = "https://tts.wangwangit.com/";
    internal const string WangwangitSpeechPath = "/v1/audio/speech";
    internal const string OkraworksUrl = "https://tts.okraworks.cn/?api_key=okraworks";
    internal const string OkraworksSpeechPath = "/api/v1/audio/speech";
    internal const string DefaultVoice = "zh-CN-XiaoxiaoNeural";
    internal const string DefaultStyle = "general";
    internal const string DefaultApiKey = "okraworks";
    internal const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0";

    internal static readonly IReadOnlyList<VoyceStyleOption> StyleOptions = new[]
    {
        new VoyceStyleOption("general", "通用"),
        new VoyceStyleOption("assistant", "智能助手"),
        new VoyceStyleOption("chat", "聊天对话"),
        new VoyceStyleOption("customerservice", "客服专业"),
        new VoyceStyleOption("newscast", "新闻播报"),
        new VoyceStyleOption("affectionate", "亲切温暖"),
        new VoyceStyleOption("calm", "平静舒缓"),
        new VoyceStyleOption("cheerful", "愉快欢乐"),
        new VoyceStyleOption("gentle", "温和柔美"),
        new VoyceStyleOption("lyrical", "抒情诗意"),
        new VoyceStyleOption("serious", "严肃正式"),
    };

    internal static readonly IReadOnlyList<VoyceEndpointOption> EndpointOptions = new[]
    {
        new VoyceEndpointOption(TtsEndpointPreset.Wangwangit, "Wangwangit", WangwangitUrl, string.Empty),
        new VoyceEndpointOption(TtsEndpointPreset.Okraworks, "Okraworks", OkraworksUrl, DefaultApiKey),
        new VoyceEndpointOption(TtsEndpointPreset.Custom, "自定义", WangwangitUrl, string.Empty),
    };

    internal static string ResolveSpeechUrl(string? url, TtsEndpointPreset preset = TtsEndpointPreset.Wangwangit)
    {
        if (string.IsNullOrWhiteSpace(url))
            return GetSpeechUrlForPreset(preset);

        var normalized = url.Trim();
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            return normalized;

        var path = uri.AbsolutePath.TrimEnd('/');
        if (IsSpeechPath(path))
            return TrimTrailingSlash(uri.AbsoluteUri);

        if (string.IsNullOrEmpty(path) || path == "/")
        {
            var builder = new UriBuilder(uri)
            {
                Path = GetSpeechPathForPreset(preset),
            };
            return TrimTrailingSlash(builder.Uri.AbsoluteUri);
        }

        return TrimTrailingSlash(uri.AbsoluteUri);
    }

    internal static VoyceSpeechRequest CreateOnlineRequest(string text, Settings settings) => new()
    {
        Voice = string.IsNullOrWhiteSpace(settings.Voice) ? DefaultVoice : settings.Voice.Trim(),
        Input = text,
        Speed = settings.Speed,
        Volume = settings.Volume,
        Pitch = settings.Pitch.ToString(CultureInfo.InvariantCulture),
        Style = string.IsNullOrWhiteSpace(settings.Style) ? DefaultStyle : settings.Style.Trim(),
        Stream = false,
    };

    internal static Dictionary<string, string> CreateRequestHeaders(Settings settings)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["User-Agent"] = BrowserUserAgent,
        };

        if (settings.EndpointPreset is TtsEndpointPreset.Wangwangit or TtsEndpointPreset.Okraworks
            && TryGetOrigin(settings.BaseUrl, out var origin))
        {
            headers["Origin"] = origin;
            headers["Referer"] = origin + "/";
        }

        var apiKey = string.IsNullOrWhiteSpace(settings.ApiKey)
            ? GetPresetApiKey(settings.EndpointPreset)
            : settings.ApiKey.Trim();

        if (!string.IsNullOrWhiteSpace(apiKey))
            headers["Authorization"] = "Bearer " + apiKey;

        return headers;
    }

    internal static TtsEndpointPreset GetPresetForUrl(string? url)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri))
            return TtsEndpointPreset.Custom;

        var host = uri.Host;
        var path = uri.AbsolutePath.TrimEnd('/');

        if (IsHost(host, "tts.wangwangit.com"))
            return path.EndsWith(WangwangitSpeechPath, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(path) || path == "/"
                ? TtsEndpointPreset.Wangwangit
                : TtsEndpointPreset.Custom;

        if (IsHost(host, "tts.okraworks.cn"))
            return path.EndsWith(OkraworksSpeechPath, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(path) || path == "/"
                ? TtsEndpointPreset.Okraworks
                : TtsEndpointPreset.Custom;

        return TtsEndpointPreset.Custom;
    }

    internal static string GetPresetUrl(TtsEndpointPreset preset) => preset switch
    {
        TtsEndpointPreset.Wangwangit => WangwangitUrl,
        TtsEndpointPreset.Okraworks => OkraworksUrl,
        _ => WangwangitUrl,
    };

    internal static string GetPresetApiKey(TtsEndpointPreset preset) => preset switch
    {
        TtsEndpointPreset.Okraworks => DefaultApiKey,
        _ => string.Empty,
    };

    private static string GetSpeechUrlForPreset(TtsEndpointPreset preset)
    {
        var baseUrl = GetPresetUrl(preset);
        return ResolveSpeechUrl(baseUrl, preset);
    }

    private static string GetSpeechPathForPreset(TtsEndpointPreset preset) => preset switch
    {
        TtsEndpointPreset.Wangwangit => WangwangitSpeechPath,
        TtsEndpointPreset.Okraworks => OkraworksSpeechPath,
        _ => OkraworksSpeechPath,
    };

    private static bool IsSpeechPath(string path) =>
        path.EndsWith(WangwangitSpeechPath, StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(OkraworksSpeechPath, StringComparison.OrdinalIgnoreCase);

    private static bool IsHost(string host, params string[] names) =>
        names.Any(name => host.Equals(name, StringComparison.OrdinalIgnoreCase) || host.EndsWith("." + name, StringComparison.OrdinalIgnoreCase));

    private static bool TryGetOrigin(string? url, out string origin)
    {
        origin = string.Empty;
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri))
            return false;

        origin = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }

    private static string TrimTrailingSlash(string value) => value.Length > 1 && value.EndsWith('/') ? value.TrimEnd('/') : value;
}

internal sealed record VoyceStyleOption(string Value, string Label);

internal sealed record VoyceEndpointOption(TtsEndpointPreset Value, string Label, string Url, string ApiKey);

internal sealed class VoyceSpeechRequest
{
    [JsonPropertyName("voice")]
    public string Voice { get; init; } = VoyceTtsProtocol.DefaultVoice;

    [JsonPropertyName("input")]
    public string Input { get; init; } = string.Empty;

    [JsonPropertyName("speed")]
    public double Speed { get; init; } = 1.0;

    [JsonPropertyName("volume")]
    public double Volume { get; init; }

    [JsonPropertyName("pitch")]
    public string Pitch { get; init; } = "0";

    [JsonPropertyName("style")]
    public string Style { get; init; } = VoyceTtsProtocol.DefaultStyle;

    [JsonPropertyName("stream")]
    public bool Stream { get; init; }
}
