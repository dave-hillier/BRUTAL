namespace Brutal.Collections.Tests;

/// <summary>
/// Tests for the untyped, byte-addressed <see cref="NativeList"/>.
/// </summary>
public class UntypedNativeListTests
{
    [Fact]
    public void CapacityThroughINativeList_ReportsCapacityNotSize()
    {
        using var list = new NativeList(ByteSize.Of(16));
        list.Add((byte)1);

        var capacity = ((INativeList)list).Capacity;

        Assert.Equal(16, (int)capacity);
    }
}
