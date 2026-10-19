using Brutal.Attributes;
using Brutal.Memory;
using Brutal.Pointers;
using CommunityToolkit.HighPerformance;

namespace Brutal.Collections;

/// <summary>
/// A native-memory backed list with no fixed type. It is byte-addressed: sizes, indices and
/// capacities are in bytes and its stride is one byte, but values of any unmanaged type can be
/// added, inserted and read.
/// </summary>
/// <remarks>
/// This is the only type in the family that owns native memory, so it is the only one with a finalizer.
/// </remarks>
public sealed class NativeList : INativeList
{
    /// <summary>
    /// The capacity in bytes when first growing from empty
    /// </summary>
    private const int DEFAULT_CAPACITY = 16;

    private ByteSize _byteSize;
    private ByteSize _byteCapacity;

    /// <summary>
    /// Create an empty list with the given initial capacity in bytes
    /// </summary>
    public NativeList(ByteSize initialCapacity = default)
    {
        Reallocate(initialCapacity);
    }

    /// <summary>
    /// Create a list holding a copy of the given bytes
    /// </summary>
    public NativeList(ReadOnlySpan<byte> initialData)
        : this(ByteSize.Of(initialData))
    {
        AddRange(initialData);
    }

    ~NativeList()
    {
        Free();
    }

    /// <inheritdoc />
    public Ptr Ptr { get; private set; }

    /// <summary>
    /// The number of bytes in the list. Growing it zero-fills the new bytes.
    /// </summary>
    public ByteSize Size
    {
        get => _byteSize;
        set
        {
            if (value > _byteSize)
            {
                EnsureCapacity(value);
                Mem.Clear(Ptr.Offset(_byteSize), value - _byteSize);
            }

            _byteSize = value;
        }
    }

    /// <summary>
    /// The number of bytes the list can hold without reallocating
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">If the new capacity is less than Size</exception>
    public ByteSize Capacity
    {
        get => _byteCapacity;
        set
        {
            if (value < _byteSize)
                throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than or equal to Size.");

            Reallocate(value);
        }
    }

    public bool IsEmpty => _byteSize == 0;

    /// <inheritdoc />
    public bool IsNull() => Ptr.IsNull();

    ByteSize ISpanLike.Stride => ByteSize.One;
    ElementCount ISpanLike.Length => ElementCount.Of(_byteSize);

    ElementCount INativeList.Count
    {
        get => ElementCount.Of(_byteSize);
        set => Size = ByteSize.Of(value);
    }

    ElementCount INativeList.Capacity
    {
        get => ElementCount.Of(_byteCapacity);
        set => Capacity = ByteSize.Of(value);
    }

    void INativeList.EnsureCapacity(ElementCount capacity) => EnsureCapacity(ByteSize.Of(capacity));

    /// <summary>
    /// Get or set the byte at an index
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">If the index is out of range</exception>
    public byte this[int index]
    {
        get => AsSpan<byte>()[index];
        set => AsSpan<byte>()[index] = value;
    }

    [return: Fixed]
    public Span<T> AsSpan<T>()
        where T : unmanaged
    {
        return Ptr.AsSpan<byte>(ElementCount.Of(_byteSize)).Cast<byte, T>();
    }

    public void Add(byte item) => Add<byte>(item);

    /// <summary>
    /// Append the bytes of any unmanaged value
    /// </summary>
    public void Add<T>(T data)
        where T : unmanaged
    {
        Insert(_byteSize, data);
    }

    /// <summary>
    /// Append the bytes of a span of unmanaged values
    /// </summary>
    public void AddRange<T>(ReadOnlySpan<T> items)
        where T : unmanaged
    {
        Mem.Copy(items, OpenGap(_byteSize, ByteSize.Of(items)).AsSpan<T>(items.Length));
    }

    /// <summary>
    /// Append the bytes of any span-like
    /// </summary>
    public void AddRange<T>(T items)
        where T : ISpanLike, allows ref struct
    {
        AddRange<byte>(items.AsSpan<byte>());
    }

    public void Insert(ByteSize index, byte item) => Insert<byte>(index, item);

    /// <summary>
    /// Insert the bytes of any unmanaged value at a byte index, shifting all later bytes up
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">If the index is greater than Size</exception>
    public void Insert<T>(ByteSize index, T item)
        where T : unmanaged
    {
        OpenGap(index, ByteSize.Of<T>()).Cast<T>().Value = item;
    }

    /// <summary>
    /// Remove a byte at an index, shifting all later bytes down
    /// </summary>
    public void RemoveAt(ByteSize index) => RemoveAt(index, ByteSize.One);

    /// <summary>
    /// Remove a number of bytes at an index, shifting all later bytes down
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">If the range does not fit inside the list</exception>
    public void RemoveAt(ByteSize index, ByteSize numBytes)
    {
        CheckRange(index, numBytes);
        var at = Ptr.Offset(index);
        Mem.Copy(at.Offset(numBytes), at, _byteSize - index - numBytes);
        _byteSize -= numBytes;
    }

    /// <summary>
    /// Set Size to zero, keeping the capacity
    /// </summary>
    public void Clear()
    {
        _byteSize = ByteSize.Zero;
    }

    /// <summary>
    /// Ensure the list can hold at least the given number of bytes, growing geometrically if it cannot
    /// </summary>
    public void EnsureCapacity(ByteSize capacity)
    {
        if (capacity <= _byteCapacity)
            return;

        var grown = ByteSize.Max(_byteCapacity * 2, ByteSize.Of(DEFAULT_CAPACITY));
        Reallocate(ByteSize.Max(grown, capacity));
    }

    /// <summary>
    /// Reallocate the list so its capacity matches its size
    /// </summary>
    public void TrimExcess() => Reallocate(_byteSize);

    /// <summary>
    /// Empty the list and free its memory. Safe to call more than once; the list can be reused afterwards.
    /// </summary>
    public void Free()
    {
        _byteSize = ByteSize.Zero;
        Reallocate(ByteSize.Zero);
    }

    /// <summary>
    /// Free the memory. Safe to call more than once.
    /// </summary>
    public void Dispose()
    {
        Free();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Make room for length bytes at index by shifting later bytes up, and return a pointer to the gap
    /// </summary>
    private Ptr OpenGap(ByteSize index, ByteSize length)
    {
        CheckRange(index, ByteSize.Zero);
        EnsureCapacity(_byteSize + length);
        var at = Ptr.Offset(index);
        Mem.Copy(at, at.Offset(length), _byteSize - index);
        _byteSize += length;
        return at;
    }

    /// <summary>
    /// Check that the byte range [index, index + length) lies within the list, without unsigned overflow
    /// </summary>
    private void CheckRange(ByteSize index, ByteSize length)
    {
        if (index > _byteSize || length > _byteSize - index)
            throw new IndexOutOfRangeException();
    }

    /// <summary>
    /// The only place memory is allocated or freed, so Ptr and Capacity always agree
    /// </summary>
    private void Reallocate(ByteSize capacity)
    {
        if (capacity == _byteCapacity)
            return;

        if (capacity == 0)
        {
            Mem.Free(Ptr);
            Ptr = default;
        }
        else
        {
            Ptr = Mem.Realloc(Ptr, capacity);
        }

        _byteCapacity = capacity;
    }
}
