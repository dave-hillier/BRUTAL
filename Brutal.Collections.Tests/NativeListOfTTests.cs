namespace Brutal.Collections.Tests;

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
