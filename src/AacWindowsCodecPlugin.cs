using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio.Wave;
#if WINDOWS_BUILD
using NAudio.MediaFoundation;
#endif
using SoundBoard.PluginApi;

namespace AacWindowsCodecPlugin;

/// <summary>
/// <see cref="IAudioCodecPlugin"/> for AAC playback (<c>.aac</c>,
/// <c>.m4a</c>) on Windows via Media Foundation. The companion repo
/// <c>gmsb-codec-aac-ffmpeg</c> provides a cross-platform alternative.
///
/// <para><b>Platform.</b> Windows only. <see cref="SupportedPatterns"/>
/// returns an empty list off-Windows so the host's codec registry skips
/// this plugin and falls through to whichever other codec covers
/// <c>.aac</c> (e.g. the FFmpeg one). <see cref="CreateStream(string)"/>
/// also re-checks at the call site as a belt-and-braces guard.</para>
///
/// <para><b>File-only by design.</b> This plugin intentionally does NOT
/// implement the inter-plugin <see cref="CreateStream(System.IO.Stream, string)"/>
/// overload — leaves <c>SupportsStreamInput = false</c> and
/// <c>SupportedContentTypes</c> empty so transport plugins like
/// <c>codec.webstream</c> won't try to route HTTP byte streams here.
/// Reason: <c>MediaFoundationReader</c> is path-based, and adapting a
/// managed <see cref="System.IO.Stream"/> to Media Foundation requires
/// constructing an <c>IMFByteStream</c> from
/// <c>MFCreateMFByteStreamOnStream</c> (~100 lines of COM boilerplate
/// + reference counting). The cross-platform <c>codec.aac.ffmpeg</c>
/// sibling handles AAC URL streaming via FFmpeg's custom-IO callbacks —
/// installing both side-by-side gives Windows users hardware-accelerated
/// file decode AND FFmpeg-driven URL streaming.</para>
///
/// <para><b>Build.</b> <c>NAudio.Wasapi</c> (which carries the Media
/// Foundation interop — <c>MediaFoundationReader</c> /
/// <c>MediaFoundationApi</c> — in modern NAudio 2.x) is restored only
/// on Windows (conditional <c>PackageReference</c> in the csproj), so
/// the Media-Foundation-touching code paths are compiled in only when
/// the <c>WINDOWS_BUILD</c> constant is defined. On non-Windows the
/// plugin still compiles — it just throws at runtime, which mirrors the
/// platform gate's intent.</para>
/// </summary>
public sealed class AacWindowsCodecPlugin : IAudioCodecPlugin
{
    public string Id => "codec.aac.windows";
    public string Name => "AAC Codec (Windows)";
    public string Description => "Adds .aac / .m4a playback on Windows via Media Foundation. Windows only — use codec.aac.ffmpeg on macOS / Linux.";
    public string Version => PluginVersion.OfAssembly(typeof(AacWindowsCodecPlugin));
    public string Author => "Devin Sanders";

    public IEnumerable<string> SupportedPatterns => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new[] { ".aac", ".m4a" }
        : Array.Empty<string>();

    public WaveStream CreateStream(string source)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("codec.aac.windows requires Windows Media Foundation.");

#if WINDOWS_BUILD
        // MediaFoundationReader already derives from WaveStream and
        // reports CanSeek = true for local files. The host disposes
        // the returned stream when playback ends, which releases the
        // underlying Media Foundation source reader.
        return new MediaFoundationReader(source);
#else
        // Reached only if a Linux/macOS-built artifact is somehow run
        // on Windows (CI on a non-Windows runner). The Windows release
        // build defines WINDOWS_BUILD and never hits this branch.
        throw new PlatformNotSupportedException(
            "This build of codec.aac.windows was produced without the Windows Media Foundation dependency. " +
            "Install the Windows-built release artifact.");
#endif
    }

    public void Initialize(IPluginContext context)
    {
#if WINDOWS_BUILD
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Idempotent in NAudio — safe to call from multiple plugins
            // and safe to call repeatedly across host restarts.
            MediaFoundationApi.Startup();
        }
#endif
    }

    public void Shutdown()
    {
#if WINDOWS_BUILD
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            MediaFoundationApi.Shutdown();
        }
#endif
    }
}
