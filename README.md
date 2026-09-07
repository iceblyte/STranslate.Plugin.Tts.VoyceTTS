# VoyceTTS

免费 TTS 插件：优先使用 Edge-compatible 在线接口，支持 Windows SAPI 离线回退，无需用户 API Key 即可离线播放。

默认在线服务为 `https://tts.okraworks.cn`，公共 key 为 `okraworks`。这是第三方公共网关，可能限流或变更；可在设置中替换为任何兼容 `POST /api/v1/audio/speech` 的服务。

## 安装与使用

1. 从 GitHub Releases 下载 `STranslate.Plugin.Tts.VoyceTTS.spkg`。
2. 打开 STranslate，进入“设置 -> 插件 -> 安装插件”。
3. 选择下载的 `.spkg`；若程序提示重启，重启 STranslate。
4. 在“设置 -> 服务”中新建 TTS 服务并选择 `VoyceTTS`。
5. 打开插件设置选择模式和音色，然后在翻译结果的朗读按钮中使用。

默认选择“自动”模式：首先请求 Okraworks，在线服务失败时改用 Windows SAPI。需要完全离线时选择“离线”。设置页提供在线和离线试听按钮。

旧 Edge TTS 插件的在线地址、音色、语速和音调可以手动填入 VoyceTTS；插件不会改写或覆盖旧插件配置。

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
