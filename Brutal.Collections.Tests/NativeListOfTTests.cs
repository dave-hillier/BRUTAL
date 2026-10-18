using Brutal.Collections;

namespace Brutal.Collections.Tests;

public class NativeListOfTTests
{
    private static int SumWithForeach(NativeList<int> list)
    {
        var sum = 0;
        foreach (var value in list)
            sum += value;
        return sum;
    }

    [Fact]
    public void Foreach_OverTypedList_DoesNotAllocateOnManagedHeap()
    {
        using var list = new NativeList<int>();
        for (var i = 0; i < 16; i++)
            list.Add(i);

        // Warm up so JIT and tiering allocations are not measured.
        for (var i = 0; i < 100; i++)
            SumWithForeach(list);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var sum = SumWithForeach(list);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(120, sum);
        Assert.Equal(0L, allocated);
    }
}
