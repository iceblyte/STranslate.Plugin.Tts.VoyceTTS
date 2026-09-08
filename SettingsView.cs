using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace STranslate.Plugin.Tts.VoyceTTS;

internal sealed class SettingsView : UserControl, IDisposable
{
    private readonly Main _main;
    private readonly ComboBox _mode = new();
    private readonly ComboBox _endpointPreset = new();
    private readonly TextBox _baseUrl = new();
    private readonly TextBox _apiKey = new();
    private readonly TextBox _voice = new();
    private readonly ComboBox _style = new();
    private readonly TextBox _speed = new();
    private readonly TextBox _volume = new();
    private readonly TextBox _pitch = new();
    private readonly TextBox _timeout = new();
    private readonly CheckBox _fallback = new() { Content = "在线失败时自动使用 Windows SAPI" };
    private readonly ComboBox _sapiVoice = new();
    private readonly TextBox _sapiRate = new();
    private readonly TextBox _sapiVolume = new();
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private bool _syncingPreset;

    public SettingsView(Main main)
    {
        _main = main;
        var settings = main.CurrentSettings;

        _mode.ItemsSource = Enum.GetValues<TtsMode>();
        _mode.SelectedItem = settings.Mode;
        _endpointPreset.ItemsSource = VoyceTtsProtocol.EndpointOptions;
        _endpointPreset.DisplayMemberPath = nameof(VoyceEndpointOption.Label);
        _endpointPreset.SelectedValuePath = nameof(VoyceEndpointOption.Value);
        _baseUrl.Text = settings.BaseUrl;
        var inferredPreset = VoyceTtsProtocol.GetPresetForUrl(settings.BaseUrl);
        _endpointPreset.SelectedValue = inferredPreset == TtsEndpointPreset.Custom ? settings.EndpointPreset : inferredPreset;
        _apiKey.Text = !string.IsNullOrWhiteSpace(settings.ApiKey)
            ? settings.ApiKey
            : _endpointPreset.SelectedValue is TtsEndpointPreset.Okraworks
                ? VoyceTtsProtocol.DefaultApiKey
                : string.Empty;
        _voice.Text = settings.Voice;
        _style.ItemsSource = VoyceTtsProtocol.StyleOptions;
        _style.DisplayMemberPath = nameof(VoyceStyleOption.Label);
        _style.SelectedValuePath = nameof(VoyceStyleOption.Value);
        _style.SelectedValue = string.IsNullOrWhiteSpace(settings.Style) ? VoyceTtsProtocol.DefaultStyle : settings.Style;
        _speed.Text = settings.Speed.ToString(CultureInfo.InvariantCulture);
        _volume.Text = settings.Volume.ToString(CultureInfo.InvariantCulture);
        _pitch.Text = settings.Pitch.ToString(CultureInfo.InvariantCulture);
        _timeout.Text = settings.TimeoutSeconds.ToString(CultureInfo.InvariantCulture);
        _fallback.IsChecked = settings.FallbackToSapi;
        _sapiRate.Text = settings.SapiRate.ToString(CultureInfo.InvariantCulture);
        _sapiVolume.Text = settings.SapiVolume.ToString(CultureInfo.InvariantCulture);
        RefreshVoices();
        if (!string.IsNullOrWhiteSpace(settings.SapiVoice)) _sapiVoice.SelectedItem = settings.SapiVoice;

        var root = new StackPanel { Margin = new Thickness(16), MaxWidth = 620 };
        root.Children.Add(new TextBlock { Text = "VoyceTTS", FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) });
        AddField(root, "运行模式", _mode);
        AddField(root, "服务预设", _endpointPreset);
        AddField(root, "在线地址", _baseUrl);
        AddField(root, "API Key", _apiKey);
        AddField(root, "在线音色", _voice);
        AddField(root, "在线风格", _style);
        AddField(root, "在线语速 (0.5-2.0)", _speed);
        AddField(root, "在线音量 (raw, 默认 0)", _volume);
        AddField(root, "在线音调 (-50-50)", _pitch);
        AddField(root, "超时秒数 (5-300)", _timeout);
        _fallback.Margin = new Thickness(150, 5, 0, 8);
        root.Children.Add(_fallback);
        AddField(root, "SAPI 语音", _sapiVoice);
        AddField(root, "SAPI 语速 (-10-10)", _sapiRate);
        AddField(root, "SAPI 音量 (0-100)", _sapiVolume);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(150, 8, 0, 4) };
        buttons.Children.Add(CreateButton("刷新语音", (_, _) => RefreshVoices()));
        buttons.Children.Add(CreateButton("在线试听", async (_, _) => await TestAsync(true)));
        buttons.Children.Add(CreateButton("离线试听", async (_, _) => await TestAsync(false)));
        root.Children.Add(buttons);
        root.Children.Add(new TextBlock
        {
            Text = "Wangwangit 使用网页端当前协议；Okraworks 保留原始查询串。URL 保持可编辑，选中预设会自动填入对应地址。",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.72,
            Margin = new Thickness(0, 12, 0, 4)
        });
        _status.Margin = new Thickness(0, 6, 0, 0);
        root.Children.Add(_status);
        Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        HookAutoSave();
        Unloaded += (_, _) => Persist();
    }

    private static void AddField(Panel parent, string label, Control control)
    {
        var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(142) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(control, 1);
        grid.Children.Add(text);
        grid.Children.Add(control);
        parent.Children.Add(grid);
    }

    private static Button CreateButton(string text, RoutedEventHandler handler)
    {
        var button = new Button { Content = text, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(10, 5, 10, 5) };
        button.Click += handler;
        return button;
    }

    private void HookAutoSave()
    {
        _mode.SelectionChanged += (_, _) => Persist();
        _endpointPreset.SelectionChanged += (_, _) => ApplyPresetSelection();
        _sapiVoice.SelectionChanged += (_, _) => Persist();
        _style.SelectionChanged += (_, _) => Persist();
        _fallback.Checked += (_, _) => Persist();
        _fallback.Unchecked += (_, _) => Persist();
        foreach (var box in new[] { _baseUrl, _apiKey, _voice, _speed, _volume, _pitch, _timeout, _sapiRate, _sapiVolume }) box.LostFocus += (_, _) => Persist();
        _baseUrl.TextChanged += (_, _) => SyncPresetFromUrl();
    }

    private void ApplyPresetSelection()
    {
        if (_syncingPreset) return;
        _syncingPreset = true;
        try
        {
            if (_endpointPreset.SelectedItem is VoyceEndpointOption option && option.Value != TtsEndpointPreset.Custom)
            {
                _baseUrl.Text = option.Url;
                _apiKey.Text = option.ApiKey;
            }
        }
        finally
        {
            _syncingPreset = false;
        }
        Persist();
    }

    private void SyncPresetFromUrl()
    {
        if (_syncingPreset) return;
        _syncingPreset = true;
        try
        {
            var preset = VoyceTtsProtocol.GetPresetForUrl(_baseUrl.Text);
            _endpointPreset.SelectedValue = preset;
            if (_endpointPreset.SelectedItem is VoyceEndpointOption option && preset != TtsEndpointPreset.Custom)
                _apiKey.Text = option.ApiKey;
        }
        finally
        {
            _syncingPreset = false;
        }
    }

    private void RefreshVoices()
    {
        try
        {
            var voices = _main.GetSapiVoices();
            _sapiVoice.ItemsSource = voices;
            if (_sapiVoice.SelectedItem is null && voices.Count > 0) _sapiVoice.SelectedIndex = 0;
            _status.Text = voices.Count == 0 ? "未检测到 Windows 语音包。" : $"检测到 {voices.Count} 个 Windows 语音。";
        }
        catch (Exception ex) { _status.Text = "读取 Windows 语音失败：" + ex.Message; }
    }

    private async Task TestAsync(bool online)
    {
        Persist();
        try
        {
            _status.Text = online ? "正在测试在线语音..." : "正在测试离线语音...";
            if (online) await _main.PlayOnlineAsync("VoyceTTS 在线语音测试", CancellationToken.None);
            else await _main.PlaySapiAsync("VoyceTTS 离线语音测试", CancellationToken.None);
            _status.Text = online ? "在线播放成功。" : "离线播放成功。";
        }
        catch (Exception ex) { _status.Text = (online ? "在线" : "离线") + "测试失败：" + ex.Message; }
    }

    private void Persist()
    {
        var settings = _main.CurrentSettings;
        settings.Mode = _mode.SelectedItem is TtsMode mode ? mode : TtsMode.Auto;
        settings.EndpointPreset = _endpointPreset.SelectedValue is TtsEndpointPreset preset ? preset : TtsEndpointPreset.Custom;
        settings.BaseUrl = string.IsNullOrWhiteSpace(_baseUrl.Text) ? VoyceTtsProtocol.GetPresetUrl(settings.EndpointPreset) : _baseUrl.Text.Trim();
        settings.ApiKey = _apiKey.Text.Trim();
        settings.Voice = string.IsNullOrWhiteSpace(_voice.Text) ? VoyceTtsProtocol.DefaultVoice : _voice.Text.Trim();
        settings.Style = _style.SelectedValue as string ?? VoyceTtsProtocol.DefaultStyle;
        settings.Speed = ParseDouble(_speed.Text, 1, 0.5, 2);
        settings.Volume = ParseRawDouble(_volume.Text, 0);
        settings.Pitch = ParseInt(_pitch.Text, 0, -50, 50);
        settings.TimeoutSeconds = ParseInt(_timeout.Text, 30, 5, 300);
        settings.FallbackToSapi = _fallback.IsChecked == true;
        settings.SapiVoice = _sapiVoice.SelectedItem as string ?? "";
        settings.SapiRate = ParseInt(_sapiRate.Text, 0, -10, 10);
        settings.SapiVolume = ParseInt(_sapiVolume.Text, 100, 0, 100);
        _main.Save();
    }

    private static int ParseInt(string text, int fallback, int min, int max) => int.TryParse(text, out var value) ? Math.Clamp(value, min, max) : fallback;
    private static double ParseDouble(string text, double fallback, double min, double max) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? Math.Clamp(value, min, max) : fallback;
    private static double ParseRawDouble(string text, double fallback) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    public void Dispose() => Persist();
}
