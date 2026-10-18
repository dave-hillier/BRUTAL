using System.Diagnostics;

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

    private static (int ExitCode, string Output) RunScenario(string scenario)
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "Brutal.Collections.Tests.CrashRepro.dll");
        var psi = new ProcessStartInfo("dotnet", $"\"{dll}\" {scenario}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, stdout + stderr);
    }

    [Fact]
    public void Insert_AtFrontRepeatedly_KeepsCapacityAtLeastCountAndPreservesOrder()
    {
        using var list = new NativeList<int>();

        for (var i = 0; i < 10; i++)
        {
            list.Insert(0, i);
            Assert.True(
                (int)list.Capacity >= (int)list.Count,
                $"After inserting {i + 1} items, Capacity {(int)list.Capacity} < Count {(int)list.Count}"
            );
        }

        Assert.Equal([9, 8, 7, 6, 5, 4, 3, 2, 1, 0], list.AsSpan().ToArray());
    }

    [Fact]
    public void Add_AfterInsertIntoEmptyList_GrowsCapacityToHoldAllItems()
    {
        using var list = new NativeList<int>();

        list.Insert(0, 1);
        list.Add(2);

        Assert.True(
            (int)list.Capacity >= (int)list.Count,
            $"After Insert then Add, Capacity {(int)list.Capacity} < Count {(int)list.Count}; Add skipped growing"
        );
        Assert.Equal([1, 2], list.AsSpan().ToArray());
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

    [Fact]
    public void TypedNativeList_WhoseConstructorThrows_DoesNotCrashTheProcessWhenFinalized()
    {
        var (exitCode, output) = RunScenario("typed-list-ctor-throws");

        Assert.True(exitCode == 0, $"exit code {exitCode}: {output}");
    }

    [Fact]
    public void NegativeCapacity_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NativeList<int>(-1));
    }
}

public class NativeListPinningTests
{
    [Fact]
    public unsafe void Fixed_OnEmptyTypedList_YieldsNullPointerLikeSpan()
    {
        using var list = new NativeList<int>();

        var exception = Record.Exception(() =>
        {
            fixed (int* p = list)
            {
                Assert.True(p == null);
            }
        });

        Assert.Null(exception);
    }
}
