using Brutal.Collections;

namespace Brutal.Collections.Tests;

public class FreeListIndexPoolTests
{
    [Fact]
    public void Removing_an_already_free_index_does_not_cause_the_same_index_to_be_handed_out_twice()
    {
        var pool = new FreeListIndexPool();
        var index = pool.Add();
        pool.Remove(index);

        // Correct behaviour: a double remove either throws or is a no-op.
        try
        {
            pool.Remove(index);
        }
        catch (Exception)
        {
            // Throwing on double remove is acceptable.
        }

        var first = pool.Add();
        var second = pool.Add();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Removing_an_already_free_index_that_is_not_the_head_of_the_free_list_does_not_hand_out_an_index_twice()
    {
        var pool = new FreeListIndexPool();
        var a = pool.Add();
        var b = pool.Add();
        pool.Remove(a);
        pool.Remove(b); // b is now the head of the free list, a sits behind it

        // Correct behaviour: a double remove either throws or is a no-op.
        try
        {
            pool.Remove(a);
        }
        catch (Exception)
        {
            // Throwing on double remove is acceptable.
        }

        var handedOut = new[] { pool.Add(), pool.Add(), pool.Add() };

        Assert.Equal(handedOut.Length, handedOut.Distinct().Count());
    }

    [Fact]
    public void IsEmpty_is_false_for_an_occupied_slot_and_true_after_it_is_removed()
    {
        var pool = new FreeListIndexPool();
        var index = pool.Add();

        Assert.False(pool.IsEmpty(index), "Slot was just allocated by Add, so it must not be reported empty");

        pool.Remove(index);

        Assert.True(pool.IsEmpty(index), "Slot was removed, so it must be reported empty");
    }
}
