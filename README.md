# VoyceTTS

免费 TTS 插件：支持 `Wangwangit` 与 `Okraworks` 两个在线预设，外加 Windows SAPI 离线回退。

`Wangwangit` 对应 `/v1/audio/speech`，`Okraworks` 对应 `/api/v1/audio/speech?api_key=okraworks`；也可以在设置里手动填完整 speech URL。

## 安装与使用

1. 从 GitHub Releases 下载 `STranslate.Plugin.Tts.VoyceTTS.spkg`。
2. 打开 STranslate，进入“设置 -> 插件 -> 安装插件”。
3. 选择下载的 `.spkg`；若程序提示重启，重启 STranslate。
4. 在“设置 -> 服务”中新建 TTS 服务并选择 `VoyceTTS`。
5. 打开插件设置选择模式和音色，然后在翻译结果的朗读按钮中使用。

默认选择“自动”模式：首先请求在线服务，在线服务失败时改用 Windows SAPI。需要完全离线时选择“离线”。设置页提供预设切换、API Key、在线和离线试听按钮。

旧 Edge TTS 插件的在线地址、音色、语速、音量、音调和风格可以手动填入 VoyceTTS；插件不会改写或覆盖旧插件配置。

## 构建

需要 .NET 10 SDK 与 Windows。Release 构建会由 `STranslate.Plugin` SDK 生成 `.spkg`。

```powershell
dotnet build -c Release
```

## 模式

- 自动：在线失败后使用 Windows SAPI。
- 在线：仅使用 HTTP TTS。
- 离线：仅使用系统语音，不访问网络。

离线语音取决于 Windows 已安装的语音包。若没有中文语音，请在 Windows 设置中安装中文语音。

## 许可证

MIT
