using System;

namespace WiFiManager
{
    public static class ColorConsole
    {
        public static void WriteHeader(string text)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('=', text.Length + 4));
            Console.WriteLine($"  {text}  ");
            Console.WriteLine(new string('=', text.Length + 4));
            Console.ForegroundColor = originalColor;
        }

        public static void WriteSuccess(string text)
        {
            WriteColored(text, ConsoleColor.Green);
        }

        public static void WriteError(string text)
        {
            WriteColored(text, ConsoleColor.Red);
        }

        public static void WriteWarning(string text)
        {
            WriteColored(text, ConsoleColor.Yellow);
        }

        public static void WriteInfo(string text)
        {
            WriteColored(text, ConsoleColor.Cyan);
        }

        public static void WriteColored(string text, ConsoleColor color)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = originalColor;
        }

        public static void WriteColoredLine(string text, ConsoleColor color)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = originalColor;
        }
    }
}