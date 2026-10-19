using System.Runtime.CompilerServices;
using Brutal.Collections;

// Usage: CrashRepro <scenario>. Exit code 0 means the process survived.
switch (args.FirstOrDefault())
{
    case "typed-list-ctor-throws":
        TryConstruct();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine("survived");
        return 0;
    default:
        Console.Error.WriteLine("unknown scenario");
        return 2;
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void TryConstruct()
{
    try
    {
        _ = new NativeList<int>(-1);
    }
    catch (Exception e)
    {
        Console.WriteLine($"ctor threw {e.GetType().Name}");
    }
}
