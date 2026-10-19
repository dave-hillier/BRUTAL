using System.Diagnostics.CodeAnalysis;

namespace Brutal.Collections;

/// <summary>
/// A pool of unique identifiers
/// </summary>
public class Namespace<T>
{
    private readonly Lock _lock = new();

    /// <summary>
    /// A global shared pool
    /// </summary>
    public static Namespace<T> Shared { get; } = new();

    // Names compare by reference, so interned strings must never be evicted
    private readonly HashSet<string> _pool = new(StringComparer.Ordinal);
    private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _lookup;

    /// <summary>
    /// Create an empty pool
    /// </summary>
    public Namespace()
    {
        _lookup = _pool.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    internal string GetOrAdd(ReadOnlySpan<char> id)
    {
        lock (_lock)
        {
            if (_lookup.TryGetValue(id, out var existing))
                return existing;

            var key = id.ToString();
            _pool.Add(key);
            return key;
        }
    }

    /// <summary>
    /// Try to get an existing ID from the pool
    /// </summary>
    public bool TryGet(ReadOnlySpan<char> id, [NotNullWhen(true)] out Name<T>? existing)
    {
        lock (_lock)
        {
            if (_lookup.TryGetValue(id, out var key))
            {
                existing = new Name<T>(key, this);
                return true;
            }

            existing = null;
            return false;
        }
    }

    /// <summary>
    /// Check if the pool contains the given ID
    /// </summary>
    public bool Contains(ReadOnlySpan<char> id)
    {
        lock (_lock)
        {
            return _lookup.Contains(id);
        }
    }
}
