using System;
using System.Collections.Generic;

namespace TeaCurses.Curses;

/// <summary>
/// Trap kinds Trappist may remix/morph among. Mirrors playable RRTrapType values
/// (excludes Wind/Freeze/DebugLock).
/// </summary>
public enum TrappistTrapKind
{
    Coals = 0,
    PortalIn = 1,
    PortalOut = 2,
    Bounce = 3,
    Mystery = 4,
}

/// <summary>Spawn snapshot for remix rules (no Unity / game types).</summary>
public struct TrappistSpawnData
{
    public TrappistTrapKind Type;
    public int DropX;
    public int DropRow;
    public int Health;
    public int DirectionIndex; // 0=Up .. see TrappistRules.DirectionCount; -1 = none
    public int ChildX;
    public int ChildRow;
    public bool HasChild;
}

/// <summary>One empty cell to fill when growing a cluster.</summary>
public struct TrappistDuplicateCell
{
    public int X;
    public int Y;
    public bool ShouldDuplicate;
}

/// <summary>
/// Trappist schedules and spawn/lifetime decisions.
/// Motion is cluster duplication (1×1 → 3×3), not translational drift.
/// </summary>
public static class TrappistRules
{
    public const int DirectionCount = 8;
    /// <summary>Stock lanes map to grid X 0..2 (Left/Mid/Right).</summary>
    public const int MinLaneX = 0;
    public const int MaxLaneX = 2;
    public const int MinRow = 0;
    public const int MaxRow = 8;

    /// <summary>Chebyshev radius for the 3×3 footprint (side = 2*r+1).</summary>
    public const int ClusterRadius = 1;

    private static readonly TrappistTrapKind[] RemappableKinds =
    {
        TrappistTrapKind.Coals,
        TrappistTrapKind.Bounce,
        TrappistTrapKind.Mystery,
        TrappistTrapKind.PortalIn,
    };

    public static int ClampIntensity(int intensity)
    {
        if (intensity < 1) return 1;
        if (intensity > 10) return 10;
        return intensity;
    }

    public static int DuplicatePeriodBeats(int intensity)
    {
        var i = ClampIntensity(intensity);
        if (i <= 2) return 2;
        if (i <= 5) return 1;
        return 1;
    }

    /// <summary>Max cells in the 3×3 footprint (including the root).</summary>
    public static int MaxClusterCells(int intensity)
    {
        var i = ClampIntensity(intensity);
        if (i <= 2) return 5;
        if (i <= 5) return 7;
        return 9;
    }

    /// <summary>How many new cells to try spawning each duplicate tick.</summary>
    public static int DuplicatesPerTick(int intensity)
    {
        var i = ClampIntensity(intensity);
        if (i <= 2) return 1;
        if (i <= 5) return 2;
        if (i <= 8) return 3;
        return 4;
    }

    /// <summary>Extra cells to try filling immediately at chart spawn (short-lived traps).</summary>
    public static int SpawnBurstCells(int intensity)
    {
        var i = ClampIntensity(intensity);
        if (i <= 2) return 1;
        if (i <= 5) return 2;
        if (i <= 8) return 3;
        return 5;
    }

    public static int MorphPeriodBeats(int intensity)
    {
        var i = ClampIntensity(intensity);
        if (i <= 2) return 1;
        return 1; // every beat at I≥3; first morph is also immediate
    }

    public static bool SoftDeceitEnabled(int intensity) => ClampIntensity(intensity) >= 6;

    public static bool SoftDeceitExtraBeatAfterMorph(int intensity) => ClampIntensity(intensity) >= 9;

    public static float TypeChangeChance(int intensity)
    {
        var i = ClampIntensity(intensity);
        if (i <= 2) return 0.2f;
        if (i <= 5) return 0.6f;
        return 0.95f;
    }

    public static bool CanDuplicate(TrappistTrapKind kind)
    {
        // Cluster cells themselves (never Portal*).
        return kind == TrappistTrapKind.Coals
            || kind == TrappistTrapKind.Bounce
            || kind == TrappistTrapKind.Mystery;
    }

    /// <summary>Any chart root except PortalOut can grow a cluster around itself.</summary>
    public static bool CanGrowCluster(TrappistTrapKind rootKind)
    {
        return rootKind != TrappistTrapKind.PortalOut;
    }

    public static bool IsAllowedTarget(TrappistTrapKind kind)
    {
        return kind == TrappistTrapKind.Coals
            || kind == TrappistTrapKind.Bounce
            || kind == TrappistTrapKind.Mystery
            || kind == TrappistTrapKind.PortalIn
            || kind == TrappistTrapKind.PortalOut;
    }

    public static bool IsRemappablePrimary(TrappistTrapKind kind)
    {
        return kind == TrappistTrapKind.Coals
            || kind == TrappistTrapKind.Bounce
            || kind == TrappistTrapKind.Mystery
            || kind == TrappistTrapKind.PortalIn;
    }

    /// <summary>
    /// Remix chart spawn data. Only chooses types present in <paramref name="loaded"/>.
    /// PortalOut chart entries are left alone (paired spawn from PortalIn).
    /// </summary>
    public static TrappistSpawnData RemixSpawn(
        TrappistSpawnData spawn,
        int intensity,
        IReadOnlyList<TrappistTrapKind> loaded,
        Random rng)
    {
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));
        if (loaded == null)
            throw new ArgumentNullException(nameof(loaded));

        intensity = ClampIntensity(intensity);

        if (spawn.Type == TrappistTrapKind.PortalOut)
            return spawn;

        var result = spawn;
        result.Health = RemixHealth(spawn.Health, intensity, rng);

        if (spawn.DirectionIndex >= 0 || spawn.Type == TrappistTrapKind.Bounce)
            result.DirectionIndex = rng.Next(0, DirectionCount);

        var candidates = CollectRemappableLoaded(loaded);
        if (candidates.Count > 0 && rng.NextDouble() < TypeChangeChance(intensity))
        {
            var pick = candidates[rng.Next(0, candidates.Count)];
            result.Type = pick;
        }
        else if (!IsAllowedTarget(result.Type) || !IsLoaded(loaded, result.Type))
        {
            if (candidates.Count > 0)
                result.Type = candidates[rng.Next(0, candidates.Count)];
        }

        if (result.Type == TrappistTrapKind.PortalIn)
        {
            result.HasChild = true;
            if (intensity >= 3 || !spawn.HasChild)
            {
                result.ChildX = ClampLane(rng.Next(MinLaneX, MaxLaneX + 1));
                result.ChildRow = ClampRow(rng.Next(MinRow, MaxRow + 1));
                if (result.ChildX == result.DropX && result.ChildRow == result.DropRow)
                    result.ChildX = result.DropX >= MaxLaneX ? MinLaneX : result.DropX + 1;
            }
            else
            {
                result.ChildX = spawn.ChildX;
                result.ChildRow = spawn.ChildRow;
            }
        }
        else
        {
            result.HasChild = false;
        }

        if (result.Type == TrappistTrapKind.Bounce && result.DirectionIndex < 0)
            result.DirectionIndex = rng.Next(0, DirectionCount);

        return result;
    }

    public static bool IsDuplicateDue(int intensity, int currentBeat, int lastDuplicateBeat)
    {
        var period = DuplicatePeriodBeats(intensity);
        if (currentBeat < 0)
            return false;
        // First opportunity is immediate so short-lived chart traps still grow.
        if (lastDuplicateBeat < 0)
            return true;
        return currentBeat - lastDuplicateBeat >= period;
    }

    public static bool IsMorphDue(int intensity, int currentBeat, int lastMorphBeat)
    {
        var period = MorphPeriodBeats(intensity);
        if (currentBeat < 0)
            return false;
        if (lastMorphBeat < 0)
            return true;
        return currentBeat - lastMorphBeat >= period;
    }

    /// <summary>
    /// Pick the next empty cell in the 3×3 footprint around the origin.
    /// Prefers cardinal neighbors before diagonals. Skips occupied / OOB.
    /// </summary>
    public static TrappistDuplicateCell ChooseDuplicateCell(
        int originX,
        int originY,
        HashSet<(int X, int Y)> clusterOwned,
        HashSet<(int X, int Y)> occupied,
        int intensity,
        Random rng)
    {
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));
        if (clusterOwned == null)
            throw new ArgumentNullException(nameof(clusterOwned));

        intensity = ClampIntensity(intensity);
        var maxCells = MaxClusterCells(intensity);
        if (clusterOwned.Count >= maxCells)
            return new TrappistDuplicateCell { ShouldDuplicate = false };

        var cardinals = new List<(int X, int Y)>();
        var diagonals = new List<(int X, int Y)>();

        for (var dy = -ClusterRadius; dy <= ClusterRadius; dy++)
        {
            for (var dx = -ClusterRadius; dx <= ClusterRadius; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var x = originX + dx;
                var y = originY + dy;
                if (x < MinLaneX || x > MaxLaneX || y < MinRow || y > MaxRow)
                    continue;

                var cell = (x, y);
                if (clusterOwned.Contains(cell))
                    continue;
                if (occupied != null && occupied.Contains(cell))
                    continue;

                var manhattan = Math.Abs(dx) + Math.Abs(dy);
                if (manhattan == 1)
                    cardinals.Add(cell);
                else
                    diagonals.Add(cell);
            }
        }

        var pool = cardinals.Count > 0 ? cardinals : diagonals;
        if (pool.Count == 0)
            return new TrappistDuplicateCell { ShouldDuplicate = false };

        var pick = pool[rng.Next(0, pool.Count)];
        return new TrappistDuplicateCell { X = pick.X, Y = pick.Y, ShouldDuplicate = true };
    }

    /// <summary>
    /// Pick a new primary type among loaded remappable kinds (prefer not current).
    /// At intensity ≥6, prefer Bounce/Mystery over staying Coals when available.
    /// </summary>
    public static TrappistTrapKind ChooseMorphTarget(
        TrappistTrapKind current,
        IReadOnlyList<TrappistTrapKind> loaded,
        Random rng,
        int intensity = 5)
    {
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));
        if (loaded == null)
            throw new ArgumentNullException(nameof(loaded));

        intensity = ClampIntensity(intensity);
        var primary = current == TrappistTrapKind.PortalOut ? TrappistTrapKind.PortalIn : current;
        var candidates = CollectRemappableLoaded(loaded);
        var others = new List<TrappistTrapKind>();
        for (var i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] != primary)
                others.Add(candidates[i]);
        }

        if (others.Count == 0)
            return candidates.Count > 0 ? candidates[0] : primary;

        if (intensity >= 6)
        {
            var spicy = new List<TrappistTrapKind>();
            for (var i = 0; i < others.Count; i++)
            {
                if (others[i] == TrappistTrapKind.Bounce || others[i] == TrappistTrapKind.Mystery)
                    spicy.Add(others[i]);
            }

            if (spicy.Count > 0 && rng.NextDouble() < 0.65)
                return spicy[rng.Next(0, spicy.Count)];
        }

        return others[rng.Next(0, others.Count)];
    }

    /// <summary>
    /// Kind for a newly duplicated cell — independent of the root (Coals/Bounce/Mystery only).
    /// </summary>
    public static TrappistTrapKind ChooseDuplicateKind(
        IReadOnlyList<TrappistTrapKind> loaded,
        Random rng,
        int intensity = 5)
    {
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));
        if (loaded == null)
            throw new ArgumentNullException(nameof(loaded));

        intensity = ClampIntensity(intensity);
        var pool = new List<TrappistTrapKind>();
        if (IsLoaded(loaded, TrappistTrapKind.Coals))
            pool.Add(TrappistTrapKind.Coals);
        if (IsLoaded(loaded, TrappistTrapKind.Bounce))
            pool.Add(TrappistTrapKind.Bounce);
        if (IsLoaded(loaded, TrappistTrapKind.Mystery))
            pool.Add(TrappistTrapKind.Mystery);

        if (pool.Count == 0)
            return TrappistTrapKind.Coals;

        if (intensity >= 6)
        {
            var spicy = new List<TrappistTrapKind>();
            for (var i = 0; i < pool.Count; i++)
            {
                if (pool[i] == TrappistTrapKind.Bounce || pool[i] == TrappistTrapKind.Mystery)
                    spicy.Add(pool[i]);
            }

            if (spicy.Count > 0 && rng.NextDouble() < 0.7)
                return spicy[rng.Next(0, spicy.Count)];
        }

        return pool[rng.Next(0, pool.Count)];
    }

    public static bool IsCloaked(
        int intensity,
        int currentBeat,
        int cloakUntilBeatInclusive)
    {
        if (!SoftDeceitEnabled(intensity))
            return false;
        if (cloakUntilBeatInclusive < 0)
            return false;
        return currentBeat <= cloakUntilBeatInclusive;
    }

    public static int CloakUntilBeatAfterMorph(int intensity, int morphBeat)
    {
        if (!SoftDeceitEnabled(intensity))
            return -1;
        if (SoftDeceitExtraBeatAfterMorph(intensity))
            return morphBeat + 1;
        return morphBeat;
    }

    public static int ClampLane(int x)
    {
        if (x < MinLaneX) return MinLaneX;
        if (x > MaxLaneX) return MaxLaneX;
        return x;
    }

    public static int ClampRow(int y)
    {
        if (y < MinRow) return MinRow;
        if (y > MaxRow) return MaxRow;
        return y;
    }

    private static int RemixHealth(int health, int intensity, Random rng)
    {
        if (health < 1)
            health = 1;

        // Never shorten chart traps — short-lived ones need every beat to grow/morph.
        if (intensity <= 2)
            return health + Math.Max(0, rng.Next(0, 2));

        if (intensity <= 5)
            return health + Math.Max(0, rng.Next(0, 3));

        if (intensity <= 8)
        {
            var factor = 1f + (float)rng.NextDouble(); // 1.0..2.0
            return Math.Max(health, (int)Math.Round(health * factor));
        }

        var hard = 1f + (float)rng.NextDouble() * 1.5f; // 1.0..2.5
        return Math.Max(health, (int)Math.Round(health * hard));
    }

    private static List<TrappistTrapKind> CollectRemappableLoaded(IReadOnlyList<TrappistTrapKind> loaded)
    {
        var list = new List<TrappistTrapKind>();
        for (var i = 0; i < RemappableKinds.Length; i++)
        {
            var k = RemappableKinds[i];
            if (IsLoaded(loaded, k))
                list.Add(k);
        }

        if (list.Contains(TrappistTrapKind.PortalIn) && !IsLoaded(loaded, TrappistTrapKind.PortalOut))
            list.Remove(TrappistTrapKind.PortalIn);

        return list;
    }

    private static bool IsLoaded(IReadOnlyList<TrappistTrapKind> loaded, TrappistTrapKind kind)
    {
        for (var i = 0; i < loaded.Count; i++)
        {
            if (loaded[i] == kind)
                return true;
        }

        return false;
    }
}
