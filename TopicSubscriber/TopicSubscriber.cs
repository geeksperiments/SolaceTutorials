using SolaceSystems.Solclient.Messaging;
using System.Threading;

namespace TopicSubscriber
{
    internal class TopicSubscriber
    {
        private EventWaitHandle WaitEventWaitHandle = new AutoResetEvent(false);

        string VPNName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: TopicSubscriber <host> <username>@<vpnname> <password>");
                Environment.Exit(1);
            }

            string[] split = args[1].Split('@');
            if (split.Length != 2)
            {
                Console.WriteLine("Usage: TopicSubscriber <host> <username>@<vpnname> <password>");
                Environment.Exit(1);
            }

            string host = args[0]; // Solace messaging router host name or IP address
            string username = split[0];
            string vpnname = split[1];
            string password = args[2];

            ContextFactoryProperties cfp = new ContextFactoryProperties();
            cfp.SolClientLogLevel = SolLogLevel.Notice;
            cfp.LogToConsoleError();

            ContextFactory.Instance.Init(cfp);
            try
            {
                // Context must be created first
                using (IContext context = ContextFactory.Instance.CreateContext(new ContextProperties(), null))
                {
                    Console.WriteLine("Context Created");
                    // Create the application
                    TopicSubscriber topicSubscriber = new TopicSubscriber()
                    {
                        VPNName = vpnname,
                        UserName = username,
                        Password = password
                    };
                    {
                        // Run the application within the context and against the host
                        topicSubscriber.Run(context, host);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown: {0}", ex.Message);
            }
            finally
            {
                // Dispose Solace Systems Messaging API
                ContextFactory.Instance.Cleanup();
            }
            Console.WriteLine("Finished.");
        }

        void Run(IContext context, string host)
        {
            // Create session properties
            SessionProperties sessionProps = new SessionProperties()
            {
                Host = host,
                VPNName = VPNName,
                AuthenticationScheme = AuthenticationSchemes.BASIC,
                UserName = UserName,
                Password = Password,
                ClientName = "my-subscriber-connection",
                ConnectTimeoutInMsecs = 5000,
                ConnectRetries = 2,
                ReconnectRetriesWaitInMsecs = 100,
                CompressionLevel = 5,
                KeepAliveIntervalInMsecs = 0,
                KeepAliveIntervalsLimit = 3
            };

            // Connect to the Solace messaging router
            Console.WriteLine("Connecting as {0}@{1} on {2}...", UserName, VPNName, host);
            // NOTICE HandleMessage as the message event handler
            using (ISession session = context.CreateSession(sessionProps, null, null))
            {
                ReturnCode returnCode = session.Connect();
                if (returnCode == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected.");
                    // This is the topic on Solace messaging router where a message is published
                    // Must subscribe to it to receive messages
                    ITopic topic = ContextFactory.Instance.CreateTopic("acme/test");
                    session.Subscribe(topic, true);

                    Console.WriteLine("Waiting for a message to be published...");
                    WaitEventWaitHandle.WaitOne();
                }
                else
                {
                    Console.WriteLine("Error connecting, return code: {0}", returnCode);
                }
            }
        }

    }

}


