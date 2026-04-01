using System;

namespace HelloCli
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello from C# CLI!");
            if (args.Length > 0)
            {
                Console.WriteLine("Arguments: " + string.Join(", ", args));
            }
        }
    }
}