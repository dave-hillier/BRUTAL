using System.Runtime.CompilerServices;
using Brutal.Attributes;
using Brutal.Memory;
using Brutal.Pointers;

namespace Brutal.Collections;

/// <summary>
/// A native-memory backed list with a fixed stride. Indices, counts and capacities are in elements.
/// </summary>
/// <remarks>
/// The memory is owned by an inner <see cref="NativeList"/>, which has its own finalizer.
/// </remarks>
public class NativeStrideList : INativeList
{
    private readonly NativeList _nativeList;

    /// <summary>
    /// Constructs a new NativeStrideList with the given stride and initial capacity
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">If the stride is zero or the capacity is negative</exception>
    public NativeStrideList(ByteSize stride, ElementCount capacity = default)
    {
        if (stride == 0)
            throw new ArgumentOutOfRangeException(nameof(stride), stride, "stride must be greater than 0");

        Stride = stride;
        _nativeList = new NativeList(ToBytes(capacity));
    }

    public static NativeStrideList FromSpan<TSpanLike>(ByteSize stride, TSpanLike span)
        where TSpanLike : ISpanLike, allows ref struct
    {
        var outList = new NativeStrideList(stride);
        outList.AddRange(span.AsSpan<byte>());
        return outList;
    }

    public static NativeStrideList FromSpan<TSpanLike>(TSpanLike span)
        where TSpanLike : ISpanLike, allows ref struct
    {
        return FromSpan(span.Stride, span);
    }

    public static NativeStrideList FromSpan<TValueType>(ByteSize stride, ReadOnlySpan<TValueType> span)
        where TValueType : unmanaged
    {
        var outList = new NativeStrideList(stride);
        outList.AddRange(span);
        return outList;
    }

    public static NativeStrideList FromSpan<TValueType>(ReadOnlySpan<TValueType> span)
        where TValueType : unmanaged
    {
        return FromSpan(ByteSize.Of<TValueType>(), span);
    }

    /// <summary>
    /// The size of each element in bytes
    /// </summary>
    public ByteSize Stride { get; }

    public bool IsEmpty => _nativeList.IsEmpty;

    /// <summary>
    /// A raw pointer to the underlying memory
    /// </summary>
    public Ptr Ptr => _nativeList.Ptr;

    ElementCount ISpanLike.Length => Count;

    /// <summary>
    /// The number of elements in the list. Growing it zero-fills the new elements.
    /// </summary>
    public ElementCount Count
    {
        get => _nativeList.Size.ToElementCount(Stride);
        set => _nativeList.Size = ToBytes(value);
    }

    /// <summary>
    /// The number of elements that can be stored in the list without resizing
    /// </summary>
    public ElementCount Capacity
    {
        get => _nativeList.Capacity.ToElementCount(Stride);
        set => _nativeList.Capacity = ToBytes(value);
    }

    /// <summary>
    /// Checks if the native list is null
    /// </summary>
    public bool IsNull() => _nativeList.IsNull();

    /// <summary>
    /// Clears the list, setting count to 0 but preserving capacity
    /// </summary>
    public void Clear() => _nativeList.Clear();

    /// <summary>
    /// Trim the capacity to the actual number of elements in the list
    /// </summary>
    public void TrimExcess() => _nativeList.TrimExcess();

    /// <summary>
    /// Ensure the list can hold at least capacity elements without resizing
    /// </summary>
    public void EnsureCapacity(ElementCount capacity) => _nativeList.EnsureCapacity(ToBytes(capacity));

    /// <summary>
    /// Frees the native memory and resets the list
    /// </summary>
    public void Free() => _nativeList.Free();

    /// <summary>
    /// Frees the native memory. Safe to call more than once.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposing)
            _nativeList.Dispose();
    }

    public NativeStrideSpan AsNativeSpan() => new(Ptr, Count, Stride);

    [return: Fixed]
    public Span<T> AsSpan<T>()
        where T : unmanaged
    {
        return _nativeList.AsSpan<T>();
    }

    public void AddRange(Span<byte> items) => AddRange((ReadOnlySpan<byte>)items);

    /// <summary>
    /// Adds a range of bytes to the end of the list. The length of items must be a multiple of stride
    /// </summary>
    /// <exception cref="ArgumentException">If the length of items is not a multiple of stride</exception>
    public void AddRange(ReadOnlySpan<byte> items)
    {
        if (ByteSize.Of(items) % Stride != 0)
            throw new ArgumentException("Items length must be a multiple of stride", nameof(items));

        _nativeList.AddRange(items);
    }

    /// <summary>
    /// Adds the bytes of a span-like to the end of the list. Its size in bytes must be a multiple of stride
    /// </summary>
    /// <exception cref="ArgumentException">If the size of items is not a multiple of stride</exception>
    public void AddRange<T>(T items)
        where T : ISpanLike, allows ref struct
    {
        AddRange((ReadOnlySpan<byte>)items.AsSpan<byte>());
    }

    /// <summary>
    /// Adds an item to the end of the list
    /// </summary>
    public void Add<T>(T item)
        where T : unmanaged
    {
        CheckStride<T>();
        _nativeList.Add(item);
    }

    public void AddRange<T>(Span<T> items)
        where T : unmanaged
    {
        AddRange((ReadOnlySpan<T>)items);
    }

    /// <summary>
    /// Adds a range of items to the end of the list
    /// </summary>
    public void AddRange<T>(ReadOnlySpan<T> items)
        where T : unmanaged
    {
        CheckStride<T>();
        _nativeList.AddRange(items);
    }

    /// <summary>
    /// Inserts an item at the given index, shifting all later items up by one
    /// </summary>
    public void Insert<T>(ElementCount index, T item)
        where T : unmanaged
    {
        CheckStride<T>();
        _nativeList.Insert(ToBytes(index), item);
    }

    /// <summary>
    /// Removes the item at the given index, shifting all later items down by one
    /// </summary>
    public void RemoveAt<T>(ElementCount index)
        where T : unmanaged
    {
        CheckStride<T>();
        _nativeList.RemoveAt(ToBytes(index), Stride);
    }

    private void CheckStride<T>()
        where T : unmanaged
    {
        if (Stride != ByteSize.Of<T>())
            throw new ArgumentException($"NativeStrideList expected stride: {Stride}, but got: {ByteSize.Of<T>()}");
    }

    /// <summary>
    /// Convert an element count or index to bytes, rejecting negative values that would wrap
    /// </summary>
    private ByteSize ToBytes(ElementCount count, [CallerArgumentExpression(nameof(count))] string? paramName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count.Value, paramName);
        return count * Stride;
    }
}
