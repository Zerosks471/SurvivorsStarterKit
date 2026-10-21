using Godot;
using System.Collections.Generic;

/// <summary>
/// Spatial hash on the XZ plane. Rebuilt once per physics frame so weapons can
/// find nearby enemies without scanning or sorting the whole enemy list.
/// </summary>
internal class EnemyGrid
{
    public readonly float CellSize;

    private readonly Dictionary<long, List<Enemy>> _cells = new();
    private readonly Stack<List<Enemy>> _spareLists = new();

    /// <summary>Largest enemy body radius this frame, so radius queries reach far enough.</summary>
    private float _maxBodyRadius = 1f;

    public EnemyGrid(float cellSize = 4f)
    {
        CellSize = cellSize;
    }

    private int ToCell(float value) => Mathf.FloorToInt(value / CellSize);

    private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

    public void Rebuild(List<Enemy> enemies)
    {
        foreach (var list in _cells.Values)
        {
            list.Clear();
            _spareLists.Push(list);
        }
        _cells.Clear();
        _maxBodyRadius = 0f;

        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            if (enemy.BodyRadius > _maxBodyRadius) _maxBodyRadius = enemy.BodyRadius;
            var pos = enemy.CachedPosition;
            long key = Key(ToCell(pos.X), ToCell(pos.Z));
            if (!_cells.TryGetValue(key, out var list))
            {
                list = _spareLists.Count > 0 ? _spareLists.Pop() : new List<Enemy>(8);
                _cells[key] = list;
            }
            list.Add(enemy);
        }
    }

    /// <summary>Nearest living enemy within maxRange, searching outward ring by ring.</summary>
    public Enemy FindNearest(Vector3 origin, float maxRange)
    {
        int cx = ToCell(origin.X);
        int cz = ToCell(origin.Z);
        int maxRing = Mathf.CeilToInt(maxRange / CellSize) + 1;

        Enemy best = null;
        float bestDistSq = maxRange * maxRange;

        for (int ring = 0; ring <= maxRing; ring++)
        {
            for (int x = cx - ring; x <= cx + ring; x++)
            {
                for (int z = cz - ring; z <= cz + ring; z++)
                {
                    // Only visit the outer edge of this ring
                    if (ring > 0 && x != cx - ring && x != cx + ring && z != cz - ring && z != cz + ring)
                        continue;
                    if (!_cells.TryGetValue(Key(x, z), out var list)) continue;

                    foreach (var enemy in list)
                    {
                        if (!enemy.IsAlive) continue;
                        float distSq = FlatDistanceSquared(origin, enemy.CachedPosition);
                        if (distSq < bestDistSq)
                        {
                            bestDistSq = distSq;
                            best = enemy;
                        }
                    }
                }
            }

            // Anything in the next ring is at least ring * CellSize away
            if (best != null && bestDistSq <= (ring * CellSize) * (ring * CellSize))
                break;
        }

        return best;
    }

    /// <summary>
    /// First living enemy whose body touches a circle of this radius.
    /// Matches the old physics overlap: distance counts the enemy's own body radius.
    /// </summary>
    public Enemy FindFirstInRadius(Vector3 origin, float radius)
    {
        float reach = radius + _maxBodyRadius;
        int minX = ToCell(origin.X - reach), maxX = ToCell(origin.X + reach);
        int minZ = ToCell(origin.Z - reach), maxZ = ToCell(origin.Z + reach);

        for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                if (!_cells.TryGetValue(Key(x, z), out var list)) continue;
                foreach (var enemy in list)
                    if (Touches(origin, radius, enemy)) return enemy;
            }

        return null;
    }

    /// <summary>Adds every living enemy whose body touches the circle to results.</summary>
    public void CollectInRadius(Vector3 origin, float radius, List<Enemy> results)
    {
        float reach = radius + _maxBodyRadius;
        int minX = ToCell(origin.X - reach), maxX = ToCell(origin.X + reach);
        int minZ = ToCell(origin.Z - reach), maxZ = ToCell(origin.Z + reach);

        for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                if (!_cells.TryGetValue(Key(x, z), out var list)) continue;
                foreach (var enemy in list)
                    if (Touches(origin, radius, enemy)) results.Add(enemy);
            }
    }

    private static bool Touches(Vector3 origin, float radius, Enemy enemy)
    {
        if (!enemy.IsAlive) return false;
        float reach = radius + enemy.BodyRadius;
        return FlatDistanceSquared(origin, enemy.CachedPosition) <= reach * reach;
    }

    private static float FlatDistanceSquared(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return dx * dx + dz * dz;
    }
}
