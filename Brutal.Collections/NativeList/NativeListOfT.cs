using System.Collections;
using Brutal.Attributes;
using Brutal.Memory;
using Brutal.Pointers;

namespace Brutal.Collections;

/// <summary>
/// A native-memory backed list with a fixed type
/// </summary>
/// <remarks>
/// The memory is owned by an inner <see cref="NativeList"/>, which has its own finalizer.
/// </remarks>
/// <typeparam name="TValueType">Element type</typeparam>
public class NativeList<TValueType> : INativeList<TValueType>
    where TValueType : unmanaged
{
    private readonly NativeStrideList _nativeList;

    public NativeList(ElementCount capacity = default)
    {
        _nativeList = new NativeStrideList(Stride, capacity);
    }

    public NativeList(ReadOnlySpan<TValueType> nativeList)
    {
        _nativeList = NativeStrideList.FromSpan(Stride, nativeList);
    }

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

    public bool IsNull() => _nativeList.IsNull();

    public ByteSize Stride => ByteSize.Of<TValueType>();
    public bool IsEmpty => _nativeList.IsEmpty;

    public ref TValueType this[Index index] => ref AsSpan()[index];

    [Fixed]
    public Span<TValueType> this[Range range] => AsSpan()[range];

    public void Add(TValueType item) => _nativeList.Add(item);

    public void AddRange(Span<TValueType> items) => _nativeList.AddRange(items);

    public void AddRange(ReadOnlySpan<TValueType> items) => _nativeList.AddRange(items);

    /// <summary>
    /// Adds the elements of a typed span-like to the end of the list
    /// </summary>
    public void AddRange<T>(T items)
        where T : ISpanLike<TValueType>
    {
        _nativeList.AddRange(items.AsSpan());
    }

    public void Insert(ElementCount index, TValueType item) => _nativeList.Insert(index, item);

    public void RemoveAt(ElementCount index) => _nativeList.RemoveAt<TValueType>(index);

    public Ptr<TValueType> Ptr => _nativeList.Ptr.Cast<TValueType>();

    Ptr ISpanLike.Ptr => Ptr;
    ElementCount ISpanLike.Length => Count;

    public ElementCount Count
    {
        get => _nativeList.Count;
        set => _nativeList.Count = value;
    }

    public ElementCount Capacity
    {
        get => _nativeList.Capacity;
        set => _nativeList.Capacity = value;
    }

    public void Clear() => _nativeList.Clear();

    public void TrimExcess() => _nativeList.TrimExcess();

    public void EnsureCapacity(ElementCount capacity) => _nativeList.EnsureCapacity(capacity);

    public void Free() => _nativeList.Free();

    /// <summary>
    /// Returns an allocation-free enumerator, so foreach does not allocate
    /// </summary>
    public NativeListEnumerator<TValueType> GetEnumerator() => new(this);

    IEnumerator<TValueType> IEnumerable<TValueType>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Span<TValueType> Slice(ElementCount start, ElementCount length) => AsSpan().Slice(start, length);

    public Span<TValueType> Slice(ElementCount start) => AsSpan()[(int)start..];

    [return: Fixed]
    public Span<TValueType> AsSpan() => _nativeList.AsSpan<TValueType>();

    Span<TCastType> ISpanLike.AsSpan<TCastType>() => _nativeList.AsSpan<TCastType>();

    public NativeStrideSpan AsNativeSpan() => new(Ptr, Count, Stride);

    /// <summary>
    /// Supports <c>fixed</c>. Like <see cref="Span{T}"/>, an empty list pins to a null pointer.
    /// </summary>
    public ref TValueType GetPinnableReference() => ref AsSpan().GetPinnableReference();
}
