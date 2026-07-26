using System;
using TeaCurses.UI;
using Xunit;

namespace TeaCurses.Tests;

public class AlbumArtResolverTests
{
    [Fact]
    public void TryResolve_prefers_first_usable_preferred()
    {
        var pick = AlbumArtResolver.TryResolve(
            new[] { false, true, true },
            new[] { true, true },
            new Random(1));
        Assert.Equal(AlbumArtSourceKind.Preferred, pick.Kind);
        Assert.Equal(1, pick.Index);
    }

    [Fact]
    public void TryResolve_falls_back_to_random_when_no_preferred()
    {
        var pick = AlbumArtResolver.TryResolve(
            new[] { false, false },
            new[] { false, true, false },
            new Random(1));
        Assert.Equal(AlbumArtSourceKind.Fallback, pick.Kind);
        Assert.Equal(1, pick.Index);
    }

    [Fact]
    public void TryResolve_empty_returns_none()
    {
        var pick = AlbumArtResolver.TryResolve(
            Array.Empty<bool>(),
            Array.Empty<bool>(),
            new Random(1));
        Assert.Equal(AlbumArtSourceKind.None, pick.Kind);
        Assert.Equal(-1, pick.Index);
    }

    [Fact]
    public void TryResolve_null_rng_with_only_fallback_returns_none()
    {
        var pick = AlbumArtResolver.TryResolve(
            new[] { false },
            new[] { true },
            null);
        Assert.Equal(AlbumArtSourceKind.None, pick.Kind);
    }
}
