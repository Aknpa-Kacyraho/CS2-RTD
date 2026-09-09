using System;
using System.Diagnostics;

namespace RollTheDice.Utils;

public static class Watch
{
	public static Stopwatch Start()
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		return stopwatch;
	}

	public static void Stop(Stopwatch stopwatch)
	{
		stopwatch.Stop();
		Console.WriteLine($"==================== TASK TOOK {stopwatch.Elapsed.TotalMilliseconds}ms");
	}
}
