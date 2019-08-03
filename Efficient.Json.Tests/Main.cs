namespace Efficient.Json.Tests
{
    public static class Program
    {
        public static int Main()
        {
            ConsoleOutput output = new ConsoleOutput();

            DeserializePerfTests tests = new DeserializePerfTests(output);
            tests.EfficientText();
            tests.EfficientStream();

            return 0;
        }
    }
}
