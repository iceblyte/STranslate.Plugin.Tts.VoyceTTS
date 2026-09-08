namespace STranslate.Plugin.Tts.VoyceTTS;

public enum TtsMode { Auto, Online, Offline }

public enum TtsEndpointPreset { Wangwangit, Okraworks, Custom }

public sealed class Settings
{
    public TtsMode Mode { get; set; } = TtsMode.Auto;
    public TtsEndpointPreset EndpointPreset { get; set; } = TtsEndpointPreset.Wangwangit;
    public string BaseUrl { get; set; } = VoyceTtsProtocol.GetPresetUrl(TtsEndpointPreset.Wangwangit);
    public string ApiKey { get; set; } = "";
    public string Voice { get; set; } = VoyceTtsProtocol.DefaultVoice;
    public string Style { get; set; } = VoyceTtsProtocol.DefaultStyle;
    public double Speed { get; set; } = 1.0;
    public double Volume { get; set; }
    public int Pitch { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool FallbackToSapi { get; set; } = true;
    public string SapiVoice { get; set; } = "";
    public int SapiRate { get; set; }
    public int SapiVolume { get; set; } = 100;
}
