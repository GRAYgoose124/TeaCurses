using System.Collections.Generic;

namespace TeaCurses.Curses;

/// <summary>
/// Mode 9: three galaxy-style spiral arms.
/// Begin together at the center, wind outward 120° apart, action-row tiles sit at the arm tips.
/// </summary>
public static class AlltheWaysTripleArmOutLayout
{
    public static float CenterX => 0f;
    public static float CenterZ => AlltheWaysMode.TurnRow + 2.5f;

    /// <summary>Base tip depth; actual tip arm distance is TipDistance + TurnRow.</summary>
    public const int TipDistance = 12;

    /// <summary>Radians of wind per arm step.</summary>
    public const float WindPerStep = 0.50f;

    /// <summary>Radius growth per arm step (tiles).</summary>
    public const float RadiusPerStep = 0.40f;

    /// <summary>Steps still in the shared hub (may occupy the same rounded cell).</summary>
    public const int HubShareDistance = 2;

    /// <summary>Outer tip depth used by action-row seats (keeps the field on the separated outer spiral).</summary>
    public static int TipArmDistance => TipDistance + AlltheWaysMode.TurnRow;

    public static float ArmPhaseRadians(int col)
        => col * (float)(2.0 * System.Math.PI / 3.0);

    public static void TipLocal(int col, int numColumns, out float tipX, out float tipZ)
    {
        _ = numColumns;
        ArmPoint(col, TipArmDistance, out tipX, out tipZ);
    }

    public static void Origin(int col, out float originX, out float originZ)
    {
        _ = col;
        originX = CenterX;
        originZ = CenterZ;
    }

    /// <summary>
    /// Point along arm <paramref name="col"/> at step <paramref name="distance"/> (≥1).
    /// Step 1 is near the shared center; TipArmDistance is the outer action tip.
    /// </summary>
    public static void ArmPoint(int col, int distance, out float localX, out float localZ)
    {
        if (distance < 1)
            distance = 1;

        float theta = ArmPhaseRadians(col) + distance * WindPerStep;
        float radius = distance * RadiusPerStep;
        localX = CenterX + radius * (float)System.Math.Sin(theta);
        localZ = CenterZ + radius * (float)System.Math.Cos(theta);
    }

    /// <summary>
    /// Map grid row → arm distance: high rows near center, row 0 at the tip.
    /// Uses TipArmDistance so the chart sits on the outer, separated spiral
    /// (monotonic — no TurnRow fold that swapped last approach tiles).
    /// </summary>
    public static int ArmDistanceForRow(int row)
    {
        if (row < 0)
            return 1;
        int d = TipArmDistance - row;
        return d < 1 ? 1 : d;
    }

    public static void LocalXZ(
        int col,
        int row,
        int numColumns,
        out float localX,
        out float localZ)
    {
        float stockX = AlltheWaysDiagonalLayout.StockLocalX(col, numColumns);
        if (row < 0)
        {
            localX = stockX;
            localZ = row;
            return;
        }

        // Action row + last approach rows sit on the outer tips / near-tips.
        // Far rows sit near the shared center and spiral out as they approach.
        ArmPoint(col, ArmDistanceForRow(row), out localX, out localZ);
    }

    public static bool TryBuildOccupancy(
        int numColumns,
        int minRow,
        int maxRow,
        out Dictionary<(int x, int z), int> owner,
        out string ascii)
    {
        owner = new Dictionary<(int x, int z), int>();
        var conflicts = new List<string>();

        for (int col = 0; col < numColumns; col++)
        {
            for (int row = minRow; row <= maxRow; row++)
            {
                if (row < 0)
                    continue;

                int dist = ArmDistanceForRow(row);
                LocalXZ(col, row, numColumns, out float fx, out float fz);
                // Quantize coarsely — spiral is continuous. Hub steps may share a cell
                // (arms begin together); past the hub, full-tile double-claims are not OK.
                var key = ((int)System.Math.Round(fx), (int)System.Math.Round(fz));
                if (owner.TryGetValue(key, out int other) && other != col)
                {
                    if (dist > HubShareDistance)
                        conflicts.Add($"cell {key} claimed by col {other} and {col} at row {row} dist={dist}");
                }
                else
                    owner[key] = col;
            }
        }

        ascii = FormatAscii(owner);
        if (conflicts.Count > 0)
            ascii = string.Join("\n", conflicts) + "\n" + ascii;
        return conflicts.Count == 0;
    }

    public static string FormatAscii(Dictionary<(int x, int z), int> owner)
    {
        if (owner.Count == 0)
            return "(empty)";

        int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue;
        foreach (var (x, z) in owner.Keys)
        {
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Galaxy arms x=[{minX},{maxX}] z=[{minZ},{maxZ}]");
        for (int z = maxZ; z >= minZ; z--)
        {
            for (int x = minX; x <= maxX; x++)
                sb.Append(owner.TryGetValue((x, z), out int col) ? (char)('0' + col) : '.');
            sb.AppendLine($"  z={z}");
        }
        return sb.ToString();
    }
}
