using System.Speech.Synthesis;
using System.IO;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using STranslate.Plugin;

namespace STranslate.Plugin.Tts.VoyceTTS;

public sealed class Main : ITtsPlugin
{
    private IPluginContext _context = null!;
    private Settings _settings = null!;
    private SettingsView? _view;

    public void Init(IPluginContext context)
    {
        _context = context;
        _settings = context.LoadSettingStorage<Settings>();
    }

    public Control GetSettingUI() => _view ??= new SettingsView(this);
    public void Dispose() => _view?.Dispose();

    public async Task PlayAudioAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (_settings.Mode != TtsMode.Offline)
        {
            try { await PlayOnlineAsync(text, cancellationToken); return; }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _context.Logger.LogWarning(ex, "VoyceTTS online synthesis failed");
                if (_settings.Mode == TtsMode.Online || !_settings.FallbackToSapi) throw;
            }
        }
        await PlaySapiAsync(text, cancellationToken);
    }

    internal async Task PlayOnlineAsync(string text, CancellationToken cancellationToken)
    {
        var baseUrl = _settings.BaseUrl.Trim().TrimEnd('/');
        var url = baseUrl.EndsWith("/audio/speech", StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : baseUrl + "/api/v1/audio/speech";
        var body = new { voice = _settings.Voice, input = text, speed = _settings.Speed, pitch = _settings.Pitch, stream = false };
        var options = new Options { Timeout = TimeSpan.FromSeconds(Math.Clamp(_settings.TimeoutSeconds, 5, 300)) };
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            options.Headers = new() { ["Authorization"] = "Bearer " + _settings.ApiKey };
        var bytes = await _context.HttpService.PostAsBytesAsync(url, body, options, cancellationToken);
        if (bytes.Length == 0) throw new InvalidOperationException("TTS 服务返回空音频");
        await _context.AudioPlayer.PlayAsync(new AudioData(bytes, AudioFormat.Mp3), cancellationToken);
    }

    internal async Task PlaySapiAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var synth = new SpeechSynthesizer();
        var installedVoices = synth.GetInstalledVoices().Where(v => v.Enabled).ToArray();
        if (installedVoices.Length == 0)
            throw new InvalidOperationException("Windows 未安装可用语音，请在系统的语言和语音设置中安装语音包。");

        synth.SelectVoice(ResolveSapiVoice(installedVoices));
        synth.Rate = Math.Clamp(_settings.SapiRate, -10, 10);
        synth.Volume = Math.Clamp(_settings.SapiVolume, 0, 100);
        using var stream = new MemoryStream();
        synth.SetOutputToWaveStream(stream);

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        synth.SpeakCompleted += (_, e) =>
        {
            if (e.Cancelled) completion.TrySetCanceled(cancellationToken);
            else if (e.Error is not null) completion.TrySetException(e.Error);
            else completion.TrySetResult();
        };
        synth.SpeakAsync(text);
        using var registration = cancellationToken.Register(synth.SpeakAsyncCancelAll);
        await completion.Task;
        var bytes = stream.ToArray();
        await _context.AudioPlayer.PlayAsync(new AudioData(bytes, AudioFormat.Wav), cancellationToken);
    }

    internal IReadOnlyList<string> GetSapiVoices()
    {
        using var synth = new SpeechSynthesizer();
        return synth.GetInstalledVoices().Where(v => v.Enabled).Select(v => v.VoiceInfo.Name).OrderBy(x => x).ToArray();
    }

    private string ResolveSapiVoice(IReadOnlyList<InstalledVoice> voices)
    {
        if (!string.IsNullOrWhiteSpace(_settings.SapiVoice) && voices.Any(v => v.VoiceInfo.Name == _settings.SapiVoice))
            return _settings.SapiVoice;
        return voices.FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))?.VoiceInfo.Name
            ?? voices.FirstOrDefault(v => v.VoiceInfo.Culture.Equals(System.Globalization.CultureInfo.CurrentUICulture))?.VoiceInfo.Name
            ?? voices[0].VoiceInfo.Name;
    }

    internal void Save() => _context.SaveSettingStorage<Settings>();
    internal Settings CurrentSettings => _settings;
}
