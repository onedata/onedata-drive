using System.Reflection;
using Microsoft.VisualBasic.FileIO;

class Program
{
    public static Config configuration = new();
    public static Dictionary<string, SpaceFolder> spaces = new();
    public static FileWatcher watcher = new();
    public static int Main(string[] args)
    {
        // set working directory
        string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        Directory.SetCurrentDirectory(exeDir);

        // input parameters
        if (args.Length == 0)
        {
            CloudSync.Run();
        }
        else if (args.Length == 1 && args[0] == "-d")
        {
            CloudSync.Run(delete: true);
        }
        else if (args.Length == 2 && args[0] == "-p")
        {
            CloudSync.Run(args[1]);
        }
        else if (args.Length == 3 && args[0] == "-p" && args[2] == "-d"
                ||
                args.Length == 3 && args[0] == "-d" && args[2] == "-p")
        {
            CloudSync.Run(path: args[1], delete: true);
        }
        else if (args.Length == 2 && args[0] == "-r")
        {
            CloudSync.Repair(args[1]);
        }
        else if (args.Length == 1 && args[0] == "-r")
        {
            CloudSync.Repair();
        }
        else
        {
            Console.WriteLine("Allowed parameters are: ");
            Console.WriteLine("------------------------------");
            Console.WriteLine(@"-d
                Delete SyncRoot folder at launch. Can be used in combination with parameter '-p'");
            Console.WriteLine("");
            Console.WriteLine(@"-p %PATH%
                Launches app and looks for config file at %PATH%");
            Console.WriteLine("");
            Console.WriteLine(@"-r
                If app did not terminate normally and you can still see SyncRoot(Onedata), use this option to remove it.");
            Console.WriteLine("");
            Console.WriteLine(@"-r %ID%
                If app did not terminate normally and you can still see SyncRoot(Onedata), use this option to remove it.
                %ID% of the SyncRoot");
            Console.WriteLine("");
            Console.WriteLine(@"If launched without parameter '-p' app looks for config.json file in the launch folder");
            Console.WriteLine("");
        }
        
        return 0;
    }
}