
using System;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using static SolaceSystems.Solclient.Messaging.SessionProperties;


namespace QueueConsumer
{
    class QueueConsumer
    {

        public static int msgCount = 0;
        private EventWaitHandle WaitEventWaitHandle = new AutoResetEvent(false);
        private IFlow flow = null;
        string VPNName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: QueueConsumer <host> <username>@<vpnname> <password>");
                Environment.Exit(1);
            }

            string[] split = args[1].Split('@');
            if (split.Length != 2)
            {
                Console.WriteLine("Usage: QueueConsumer <host> <username>@<vpnname> <password>");
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
                    QueueConsumer queueConsumer = new QueueConsumer()
                    {
                        VPNName = vpnname,
                        UserName = username,
                        Password = password
                    };
                    {
                        // Run the application within the context and against the host
                        queueConsumer.Run(context, host);
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
                ClientName = "my-queue-consumer-connection",
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
                    ReceiveMessage(session);
                }
                else
                {
                    Console.WriteLine("Error connecting, return code: {0}", returnCode);
                }
            }
        }

        private void ReceiveMessage(ISession session)
        {
            EndpointProperties endpointProps = new EndpointProperties();
            // Set permissions to allow all permissions to others.
            endpointProps.Permission = EndpointProperties.EndpointPermission.Delete;
            // Set access type to exclusive.
            endpointProps.AccessType = EndpointProperties.EndpointAccessType.Exclusive;
            // Set quota to 100 MB.
            endpointProps.Quota = 100;
            var queueName = "dev_queue";
            IQueue queue = ContextFactory.Instance.CreateQueue(queueName);
            Console.WriteLine(String.Format("About to provision queue '{0}' on the appliance", queueName));
            session.Provision(queue /* endpoint */,
                endpointProps /*endpoint properties */,
                ProvisionFlag.WaitForConfirm | ProvisionFlag.IgnoreErrorIfEndpointAlreadyExists /* block waiting for confirmation */,
                null /*no correlation key*/);
            Console.WriteLine(string.Format("Queue '{0}' successfully provisioned on the appliance", queueName));

            ITopic topic = ContextFactory.Instance.CreateTopic("acme/test");
            session.Subscribe(queue, topic, SubscribeFlag.WaitForConfirm, null);

            FlowProperties flowProps = new FlowProperties();
            flowProps.AckMode = MessageAckMode.ClientAck;
            flow = session.CreateFlow(flowProps, queue, null, HandleMessageEvent, HandleFlowEvent);

            Console.WriteLine("Waiting for a message to be published...");
            WaitEventWaitHandle.WaitOne();
        }


        public void HandleMessageEvent(Object source, MessageEventArgs args)
        {
            Console.WriteLine("Received published message.");
            // Received a message
            using (IMessage message = args.Message)
            {
                msgCount++;
                // Expecting the message content as a binary attachment
                Console.WriteLine("Message content: {0}", Encoding.ASCII.GetString(message.BinaryAttachment));

                // When AckMode is set to ClientAck, guaranteed delivery messages are acknowledged after
                // processing
                flow.Ack(message.ADMessageId);

                // finish the program
                WaitEventWaitHandle.Set();
            }
        }

        public void HandleFlowEvent(object sender, FlowEventArgs args)
        {
            // Received a flow event
            Console.WriteLine("Received Flow Event '{0}' Type: '{1}' Text: '{2}'",
                args.Event,
                args.ResponseCode.ToString(),
                args.Info);
        }

    }
}
