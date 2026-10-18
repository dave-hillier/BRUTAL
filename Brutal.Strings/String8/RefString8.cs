using System.Runtime.InteropServices.Marshalling;
using Brutal.Attributes;

namespace Brutal.Strings;

/// <summary>
/// A reference to a null-terminated string of bytes.
/// </summary>
[DebuggerDisplay("{ToString16()}")]
[NativeMarshalling(typeof(RefString8Marshaller))]
public readonly ref struct RefString8 : IString8
{
    private readonly ref Char8 _firstChar;

    // Span length the string was created from, or -1 when created from a pointer.
    private readonly int _maxLength;

    public static RefString8 Empty => String8View.Empty;

    /// <summary>
    /// Create a safe reference to a null-terminated string of bytes.
    /// Managed reads stop at the first null or the end of <paramref name="cString"/>, whichever comes first.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefString8(String8View cString)
    {
        _firstChar = ref MemoryMarshal.GetReference(cString);
        _maxLength = cString.Length;
    }

    /// <summary>
    /// Create an unsafe reference to a null-terminated string of bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe RefString8([Fixed] Ptr<Char8> pointer)
    {
        _firstChar = ref Unsafe.AsRef<Char8>(pointer);
        _maxLength = -1;
    }

    /// <summary>
    /// Get a reference to the first character of the string.
    /// </summary>
    public ref Char8 FirstChar
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _firstChar;
    }

    /// <summary>
    /// Implicit conversion from a null-terminated string of bytes to a RefString8.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RefString8([Fixed] Ptr<Char8> cString)
    {
        return new RefString8(cString);
    }

    /// <summary>
    /// Implicit conversion from a String8View to a RefString8.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RefString8(String8View bytes)
    {
        return new RefString8(bytes);
    }

    /// <summary>
    /// Implicit conversion from a Span&lt;Char8&gt; to a RefString8.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RefString8(Span<Char8> bytes)
    {
        return new RefString8(bytes);
    }

    /// <summary>
    /// Implicit conversion from a byte array to a RefString8.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RefString8(Char8[] bytes)
    {
        return new RefString8(bytes);
    }

    /// <summary>
    /// Implicit conversion from a String8 to a RefString8.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RefString8(String8 cString)
    {
        return new RefString8(cString.AsSpan());
    }

    public static implicit operator String8View(RefString8 str)
    {
        return str.Chars;
    }

    /// <summary>
    /// Get a reference to the first character of the string.
    /// </summary>
    public ref Char8 GetPinnableReference()
    {
        return ref _firstChar;
    }

    public unsafe String8View Chars
    {
        get
        {
            if (_maxLength < 0)
                return Utf8.GetNullTerminatedSpan((Ptr<Char8>)Unsafe.AsPointer(ref _firstChar));

            var bytes = MemoryMarshal.CreateReadOnlySpan(ref _firstChar, _maxLength);
            return bytes[..Utf8.EvaluateLength(bytes)];
        }
    }

    public int Length => Chars.Length;
    public bool IsEmpty => Unsafe.IsNullRef(ref _firstChar) || _maxLength == 0 || _firstChar == 0;

    public Char8 this[int index] => Chars[index];
    public String8View this[Range range] => Chars[range];

    public String8View AsSpan()
    {
        return Chars;
    }

    public String16 ToString16()
    {
        return Utf8.GetString(Chars);
    }
}
