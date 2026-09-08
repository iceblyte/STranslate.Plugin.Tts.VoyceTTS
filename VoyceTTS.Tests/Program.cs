using System.Text.Json;
using STranslate.Plugin.Tts.VoyceTTS;

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{name} expected '{expected}' but was '{actual}'");
}

static void AssertContains(string text, string fragment, string name)
{
    if (!text.Contains(fragment, StringComparison.Ordinal))
        throw new InvalidOperationException($"{name} missing '{fragment}' in '{text}'");
}

AssertEqual("https://tts.okraworks.cn/api/v1/audio/speech?api_key=okraworks", VoyceTtsProtocol.ResolveSpeechUrl("https://tts.okraworks.cn/?api_key=okraworks", TtsEndpointPreset.Okraworks), "okraworks query endpoint");
AssertEqual("https://tts.wangwangit.com/v1/audio/speech", VoyceTtsProtocol.ResolveSpeechUrl("https://tts.wangwangit.com", TtsEndpointPreset.Wangwangit), "wangwangit root endpoint");
AssertEqual("https://tts.wangwangit.com/v1/audio/speech", VoyceTtsProtocol.ResolveSpeechUrl("https://tts.wangwangit.com/v1/audio/speech", TtsEndpointPreset.Wangwangit), "explicit v1 endpoint");
AssertEqual("https://tts.wangwangit.com/api/v1/audio/speech", VoyceTtsProtocol.ResolveSpeechUrl("https://tts.wangwangit.com/api/v1/audio/speech", TtsEndpointPreset.Okraworks), "legacy endpoint");

var okraHeaders = VoyceTtsProtocol.CreateRequestHeaders(new Settings
{
    EndpointPreset = TtsEndpointPreset.Okraworks,
    ApiKey = "okraworks",
    BaseUrl = VoyceTtsProtocol.OkraworksUrl,
});

AssertEqual("Bearer okraworks", okraHeaders["Authorization"], "okraworks auth");
AssertEqual(VoyceTtsProtocol.BrowserUserAgent, okraHeaders["User-Agent"], "browser ua");

var request = VoyceTtsProtocol.CreateOnlineRequest("hello", new Settings
{
    Voice = "zh-CN-YunxiNeural",
    Style = "cheerful",
    Speed = 1.25,
    Volume = 0.5,
    Pitch = -4,
});

var json = JsonSerializer.Serialize(request);
AssertContains(json, "\"voice\":\"zh-CN-YunxiNeural\"", "voice");
AssertContains(json, "\"input\":\"hello\"", "input");
AssertContains(json, "\"speed\":1.25", "speed");
AssertContains(json, "\"volume\":0.5", "volume");
AssertContains(json, "\"pitch\":\"-4\"", "pitch");
AssertContains(json, "\"style\":\"cheerful\"", "style");
AssertContains(json, "\"stream\":false", "stream");

Console.WriteLine("VoyceTTS self-check passed.");
