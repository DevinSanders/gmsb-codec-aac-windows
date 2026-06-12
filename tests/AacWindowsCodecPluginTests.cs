using System.IO;
using System.Linq;
using AacWindowsCodecPlugin;
using FluentAssertions;
using NAudio.Wave;
using SoundBoard.PluginApi;
using Xunit;

namespace AacWindowsCodecPlugin.Tests;

/// <summary>
/// Unit tests for <see cref="AacWindowsCodecPlugin"/>. Declaration tests run
/// on any OS; decode tests require Windows Media Foundation and self-skip
/// elsewhere via <see cref="Assert.SkipUnless"/>.
/// </summary>
public class AacWindowsCodecPluginTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "fixtures");

    private static string Fixture(string name) => Path.Combine(FixtureDir, name);

    private static AacWindowsCodecPlugin NewPlugin() => new();

    // ── Declarations ──────────────────────────────────────────────────

    [Fact]
    public void Identity_fields_are_populated()
    {
        var plugin = NewPlugin();
        plugin.Id.Should().Be("codec.aac.windows");
        plugin.Name.Should().NotBeNullOrWhiteSpace();
        plugin.Author.Should().NotBeNullOrWhiteSpace();
        plugin.Description.Should().NotBeNullOrWhiteSpace();
        // Version is computed from assembly metadata, never empty.
        plugin.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SupportedPatterns_match_the_host_OS()
    {
        var patterns = NewPlugin().SupportedPatterns.ToArray();

        if (OperatingSystem.IsWindows())
            patterns.Should().BeEquivalentTo(new[] { ".aac", ".m4a" });
        else
            // Off-Windows the codec refuses to register so the host falls
            // through to a cross-platform AAC plugin.
            patterns.Should().BeEmpty();
    }

    [Fact]
    public void Stream_input_is_not_supported()
    {
        IAudioCodecPlugin plugin = NewPlugin();
        plugin.SupportsStreamInput.Should().BeFalse();
        plugin.SupportedContentTypes.Should().BeEmpty();
    }

    [Fact]
    public void CreateStream_from_a_Stream_throws_NotSupported()
    {
        IAudioCodecPlugin plugin = NewPlugin();
        using var bytes = new MemoryStream(File.ReadAllBytes(Fixture("tone.m4a")));

        var act = () => plugin.CreateStream(bytes, ".m4a");

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Encoding_and_packet_decode_are_not_supported()
    {
        IAudioCodecPlugin plugin = NewPlugin();
        plugin.SupportsEncoding.Should().BeFalse();
        plugin.CreateEncoder(48000, 2, 128000).Should().BeNull();
        plugin.CreateDecoder().Should().BeNull();
    }

    // ── Decode (Windows-only) ─────────────────────────────────────────

    [Theory]
    [InlineData("tone.m4a")]
    [InlineData("tone.aac")]
    public void CreateStream_decodes_a_fixture(string fixtureName)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(),
            "MediaFoundationReader requires Windows Media Foundation.");

        var path = Fixture(fixtureName);
        File.Exists(path).Should().BeTrue($"fixture {fixtureName} must be copied to output");

        using var stream = NewPlugin().CreateStream(path);

        stream.WaveFormat.Channels.Should().Be(2);
        stream.WaveFormat.SampleRate.Should().Be(44100);
        stream.CanSeek.Should().BeTrue("a local file is seekable");

        // The clip is 0.5 s; allow generous slack for AAC decoder priming.
        stream.TotalTime.TotalSeconds.Should().BeInRange(0.3, 0.9);

        var buffer = new byte[stream.WaveFormat.AverageBytesPerSecond / 10];
        stream.Read(buffer, 0, buffer.Length).Should().BeGreaterThan(0);
    }

    [Fact]
    public void Position_round_trips_after_seek()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(),
            "MediaFoundationReader requires Windows Media Foundation.");

        using var stream = NewPlugin().CreateStream(Fixture("tone.m4a"));

        // Read a chunk to advance the position, then seek back to the start.
        var buffer = new byte[stream.WaveFormat.AverageBytesPerSecond / 10];
        var read = stream.Read(buffer, 0, buffer.Length);
        read.Should().BeGreaterThan(0);
        stream.Position.Should().BeGreaterThan(0);

        stream.Position = 0;
        stream.Position.Should().Be(0);
    }
}
