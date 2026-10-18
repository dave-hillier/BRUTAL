using System.Runtime.InteropServices;

namespace Brutal.Memory.Tests;

/// <summary>
/// Frees are observed with macOS malloc_size, which is defined to return 0 for a pointer the
/// allocator does not own (including one that has been freed). The Linux and Windows equivalents
/// (malloc_usable_size, _msize) are undefined behaviour on a freed pointer, so these tests are
/// skipped elsewhere.
/// </summary>
public sealed class MacOSFactAttribute : FactAttribute
{
    public MacOSFactAttribute()
    {
        if (!OperatingSystem.IsMacOS())
            Skip = "Observes frees with macOS malloc_size; no portable safe equivalent";
    }
}

public unsafe class PtrOwnerTests
{
    [DllImport("libSystem.dylib")]
    static extern nuint malloc_size(void* ptr);

    [MacOSFact]
    public void PtrOwner_Reset_frees_the_previous_pointer()
    {
        var owner = new PtrOwner<long>(42L);
        var old = owner.Ptr;
        Assert.NotEqual((nuint)0, malloc_size(old.Pointer));

        owner.Reset();

        // freed blocks report a size of 0
        Assert.Equal((nuint)0, malloc_size(old.Pointer));
    }

    [MacOSFact]
    public void Control_Dispose_is_observable_via_malloc_size()
    {
        var owner = new PtrOwner<long>(42L);
        var old = owner.Ptr;
        Assert.NotEqual((nuint)0, malloc_size(old.Pointer));

        owner.Dispose();

        Assert.Equal((nuint)0, malloc_size(old.Pointer));
    }
}
