using System.Runtime.InteropServices;

namespace Brutal.Memory.Tests;

/// <summary>
/// macOS malloc_size returns 0 for a freed pointer; the Linux and Windows equivalents are undefined behaviour there.
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

        Assert.Equal((nuint)0, malloc_size(old.Pointer));
    }

    [MacOSFact]
    public void PtrOwner_Reset_with_a_new_pointer_frees_the_old_one_and_owns_the_new_one()
    {
        var owner = new PtrOwner<long>(42L);
        var old = owner.Ptr;
        var replacement = Mem.AllocAndCopy(7L);

        owner.Reset(replacement);

        Assert.Equal((nuint)0, malloc_size(old.Pointer));
        Assert.NotEqual((nuint)0, malloc_size(replacement.Pointer));

        owner.Dispose();

        Assert.Equal((nuint)0, malloc_size(replacement.Pointer));
    }

    [MacOSFact]
    public void PtrOwner_Reset_with_its_own_pointer_does_not_free_it()
    {
        var owner = new PtrOwner<long>(42L);
        // If the block were freed here, the finalizer would free it a second time.
        GC.SuppressFinalize(owner);
        var current = owner.Ptr;

        owner.Reset(owner.Ptr);

        Assert.NotEqual((nuint)0, malloc_size(current.Pointer));
        Assert.Equal((nint)current.Pointer, (nint)owner.Ptr.Pointer);
        Assert.Equal(42L, owner.Value);

        owner.Dispose();

        Assert.Equal((nuint)0, malloc_size(current.Pointer));
    }

    [Fact]
    public void PtrOwner_Reset_with_a_new_pointer_adopts_it()
    {
        using var owner = new PtrOwner<long>(42L);
        var replacement = Mem.AllocAndCopy(7L);

        owner.Reset(replacement);

        Assert.Equal((nint)replacement.Pointer, (nint)owner.Ptr.Pointer);
        Assert.Equal(7L, owner.Value);
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
