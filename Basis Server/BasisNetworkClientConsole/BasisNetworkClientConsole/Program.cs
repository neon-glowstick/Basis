using System.Text;
using static Basis.Network.Core.Serializable.SerializableBasis;
using static SerializableBasis;

namespace Basis
{
    class Program
    {
        public static void Main(string[] args)
        {
            // Set up global exception handlers
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // Get the path to the config.xml file in the application's directory
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");


            // Create a cancellation token source
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Start the server in a background task and prevent it from exiting
            Task serverTask = Task.Run(() =>
            {
                try
                {
                    ReadyMessage RM = new ReadyMessage
                    {
                        playerMetaDataMessage = new PlayerMetaDataMessage()
                    };
                    RM.playerMetaDataMessage.playerDisplayName = "Fake User";
                    RM.playerMetaDataMessage.playerUUID = "UUID Test";
                    RM.clientAvatarChangeMessage = new ClientAvatarChangeMessage
                    {
                        byteArray = new byte[13]
                    };
                    RM.localAvatarSyncMessage = new LocalAvatarSyncMessage
                    {
                        array = new byte[LocalAvatarSyncMessage.AvatarSyncSize]
                    };
                    AuthenticationMessage Authmessage = new AuthenticationMessage
                    {
                        bytes = Encoding.UTF8.GetBytes("default_password")
                    };
                    BasisNetworkClient.AuthenticationMessage = Authmessage;
                    BasisNetworkClient.StartClient("localhost", 4296, RM,true);
                    BNL.Log("Connecting!");
                }
                catch (Exception ex)
                {
                    BNL.LogError($"Server encountered an error: {ex.Message} {ex.StackTrace}");
                    // Optionally, handle server restart or log critical errors
                }
            }, cancellationToken);

            // Register a shutdown hook to clean up resources when the application is terminated
            AppDomain.CurrentDomain.ProcessExit += async (sender, eventArgs) =>
            {
                BNL.Log("Shutting down server...");

                // Perform graceful shutdown of the server and logging
                cancellationTokenSource.Cancel();

                try
                {
                    await serverTask; // Wait for the server to finish
                }
                catch (Exception ex)
                {
                    BNL.LogError($"Error during server shutdown: {ex.Message}");
                }
                BNL.Log("Server shut down successfully.");
            };

            // Keep the application running
            while (true)
            {
                Thread.Sleep(15000);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                BNL.LogError($"Fatal exception: {exception.Message}");
                BNL.LogError($"Stack trace: {exception.StackTrace}");
            }
            else
            {
                BNL.LogError("An unknown fatal exception occurred.");
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            foreach (var exception in e.Exception.InnerExceptions)
            {
                BNL.LogError($"Unobserved task exception: {exception.Message}");
                BNL.LogError($"Stack trace: {exception.StackTrace}");
            }
            e.SetObserved(); // Prevents the application from crashing
        }
    }
}
