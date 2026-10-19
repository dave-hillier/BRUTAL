using Brutal.Strings;

// Runs a Utf8.Path call on a long path out of process, since a stack overflow terminates the process.
// Usage: <GetDirectoryName|GetFileName|IsPathFullyQualified> <pathByteLength>

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
// Worker threads get the default stack size, which is smaller than the main thread's on some platforms.
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
