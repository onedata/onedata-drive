namespace OnedataDriveGUI
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.

            bool mutexOwner;
            using (Mutex mutex = new Mutex(true, "OnedataDrive", out mutexOwner))
            {
                if (mutexOwner)
                {
                    ApplicationConfiguration.Initialize();
                    Application.Run(new ConnectForm());
                }
            }    
        }
    }
}