﻿
using System;
using System.Text;
using SolaceSystems.Solclient.Messaging;


namespace QueueProducer
{
    class QueueProducer
    {
        string VPNName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: QueueProducer <host> <username>@<vpnname> <password>");
                Environment.Exit(1);
            }

            string[] split = args[1].Split('@');
            if (split.Length != 2)
            {
                Console.WriteLine("Usage: QueueProducer <host> <username>@<vpnname> <password>");
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
                    QueueProducer queueProducer = new QueueProducer()
                    {
                        VPNName = vpnname,
                        UserName = username,
                        Password = password
                    };
                    {
                        // Run the application within the context and against the host
                        queueProducer.Run(context, host);
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
                ClientName = "my-queue-producer-connection",
                ConnectTimeoutInMsecs = 5000,
                ConnectRetries = 2,
                ReconnectRetriesWaitInMsecs = 100
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
                    PublishMessage(session);
                }
                else
                {
                    Console.WriteLine("Error connecting, return code: {0}", returnCode);
                }
            }
        }

        private void PublishMessage(ISession session)
        {
            // Create the message
            using (IMessage message = ContextFactory.Instance.CreateMessage())
            {
                ITopic topic = ContextFactory.Instance.CreateTopic("acme/test");
                message.Destination = topic;
                message.DeliveryMode = MessageDeliveryMode.Persistent;
                message.BinaryAttachment = Encoding.ASCII.GetBytes("Sample Message");

                // Publish the message to the topic on the Solace messaging router
                Console.WriteLine("Publishing message...");
                ReturnCode returnCode = session.Send(message);
                if (returnCode == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Done.");
                }
                else
                {
                    Console.WriteLine("Publishing failed, return code: {0}", returnCode);
                }
            }
        }

    }
}
