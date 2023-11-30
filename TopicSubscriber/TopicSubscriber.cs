using SolaceSystems.Solclient.Messaging;

namespace TopicSubscriber
{
    internal class TopicSubscriber
    {
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
            ContextFactory.Instance.Init(cfp);
            try
            {
                using (IContext context = ContextFactory.Instance.CreateContext(new ContextProperties(), null))
                {
                    Console.WriteLine("Context Created");

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
    }

}


