using System;
using System.Diagnostics;

namespace UnitTests.Utils
{
    public static class Performance
    {
        public static double MeasureMs(Action action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Stop();
            var msPerTick = 1000.0/Stopwatch.Frequency;
            return stopwatch.ElapsedTicks*msPerTick;
        }
    }
}