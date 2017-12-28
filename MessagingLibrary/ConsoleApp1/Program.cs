namespace ConsoleApp1
{
    using BroadcastMessaging;
    using SharedTestAppCode;
    using System;
    using System.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            MessageRegistrar messageRegistrar = new MessageRegistrar();

            messageRegistrar.RegisterMessageFactory<TestGuidMessage>(
                () => new TestGuidMessage());

            messageRegistrar.RegisterMessageHandler<TestGuidMessage>(
                msg =>
                {
                    var testMessage = msg as TestGuidMessage;

                    if (testMessage != null)
                    {
                        Console.WriteLine(
                            $"\tReceived a message from {testMessage.Id}");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"\tReceived an unexpected message of type {msg.GetType().Name} - where did it come from?");
                    }
                }
            );

            var ipAddress =
                NetworkUtility.GetConnectedIpAddresses(false, true, AddressFamilyType.IP4)
                .FirstOrDefault()
                .ToString();

            Console.WriteLine(
                $"Listening on local network address {ipAddress}");

            var messageService = new MessageService(messageRegistrar, ipAddress);

            var guid = Guid.NewGuid();

            Console.WriteLine(
                $"Using local identifier {guid} for messages");

            messageService.Open();

            Console.WriteLine("Hit X to exit, S to send a message");

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.X)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.S)
                {
                    var msg = new TestGuidMessage()
                    {
                        Id = guid
                    };
                    messageService.Send(msg,
                        sent =>
                        {
                            Console.WriteLine($"\tMessage sent? {sent}");
                        }
                    );
                }
            }

            messageService.Close();
            messageService.Dispose();

            Console.WriteLine("Done");
        }
    }
}