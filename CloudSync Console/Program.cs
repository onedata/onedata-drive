// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Config config = new();
config.Init("C:\\Users\\User\\Desktop\\win-client\\local-config.json");

try
{
    CloudSync.Run(config, delete: true);

    Console.WriteLine("App is running");

    while (true)
    {
        ConsoleKeyInfo key = Console.ReadKey();
        if (key.Key == ConsoleKey.Enter)
        {
            break;
        }
    }
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
}
finally
{
    CloudSync.Stop();
}

