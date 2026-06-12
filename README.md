# gmsb-codec-aac-windows

**Windows only.**

AAC codec plugin for
[Game Master Sound Board](https://github.com/DevinSanders/game-master-soundboard)
that uses Windows Media Foundation for hardware-accelerated decode of
`.aac` and `.m4a` files. For a cross-platform AAC plugin see
[`gmsb-codec-aac-ffmpeg`](../gmsb-codec-aac-ffmpeg).

Refuses to register the `.aac` / `.m4a` extensions when the host runs
on macOS or Linux, so it can sit installed alongside the FFmpeg variant
without conflict — whichever one's actually loadable on the current OS
takes effect.

## Install

**Paid plugin.** The source is open here for reference, but the pre-built
binary is distributed pay-what-you-want on itch.io:

**→ https://dsand64.itch.io/gmsb-codec-aac-windows**

Download the `.zip` from that page and drop it onto **Settings → Plugin
Manager** in Game Master Sound Board. Restart when prompted, then enable it under **Settings → Plugins**.

## Manifest

| Field     | Value                       |
|-----------|-----------------------------|
| publisher | `github.DevinSanders`       |
| id        | `codec.aac.windows`         |
| entryDll  | `AacWindowsCodecPlugin.dll` |

## License

Released under the [MIT License](LICENSE).

Third-party components used by this plugin:

- NAudio.Wasapi (MIT) — provides `MediaFoundationReader` / `MediaFoundationApi` for AAC decoding via the Windows Media Foundation API.