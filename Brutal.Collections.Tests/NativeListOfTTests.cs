namespace Brutal.Collections.Tests;

public class NativeListOfTTests
{
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
}
