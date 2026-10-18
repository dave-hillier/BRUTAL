using Brutal.Strings;

// Out-of-process reproduction for Utf8.Path on very long paths.
// A stack overflow cannot be caught and terminates the process, so this cannot run inside
// the xUnit test host. Utf8PathTests launches this program and inspects its exit code.
//
// Usage: Brutal.Strings.Tests.CrashRepro <GetDirectoryName|GetFileName|IsPathFullyQualified> <pathByteLength>
// The call runs on a fresh worker thread with the default stack size.
// Output: "SURVIVED <result>" and exit code 0 when the call returns.

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: <method> <pathByteLength>");
    return 2;
}

var method = args[0];
var path = new byte[int.Parse(args[1])];
Array.Fill(path, (byte)'a');
path[^2] = (byte)'/';

string? result = null;
var worker = new Thread(() =>
    result = method switch
    {
        "GetDirectoryName" => Utf8.Path.GetDirectoryName(path).Length.ToString(),
        "GetFileName" => Utf8.Path.GetFileName(path).Length.ToString(),
        "IsPathFullyQualified" => Utf8.Path.IsPathFullyQualified(path).ToString(),
        _ => throw new ArgumentException($"Unknown method {method}"),
    }
);
worker.Start();
worker.Join();

Console.WriteLine($"SURVIVED {result}");
return 0;
