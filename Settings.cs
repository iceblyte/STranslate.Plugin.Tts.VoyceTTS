namespace STranslate.Plugin.Tts.VoyceTTS;

public enum TtsMode { Auto, Online, Offline }

public sealed class Settings
{
    public TtsMode Mode { get; set; } = TtsMode.Auto;
    public string BaseUrl { get; set; } = "https://tts.okraworks.cn";
    public string ApiKey { get; set; } = "okraworks";
    public string Voice { get; set; } = "zh-CN-XiaoxiaoNeural";
    public double Speed { get; set; } = 1.0;
    public int Pitch { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool FallbackToSapi { get; set; } = true;
    public string SapiVoice { get; set; } = "";
    public int SapiRate { get; set; }
    public int SapiVolume { get; set; } = 100;
}
