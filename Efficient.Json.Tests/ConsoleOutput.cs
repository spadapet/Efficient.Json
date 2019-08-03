using System;
using Xunit.Abstractions;

namespace Efficient.Json.Tests
{
    internal class ConsoleOutput : ITestOutputHelper
    {
        void ITestOutputHelper.WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        void ITestOutputHelper.WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}
