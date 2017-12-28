namespace ConsoleFlightRecorder
{
    using BroadcastMessaging;
    using SharedHolograms.Messages;
    using System;
    using System.Collections.Generic;
    using System.Linq;


    class Program
    {
        static void Main(string[] args)
        {
            var registrar = new MessageRegistrar();

            List<Message> messageList = new List<Message>();

            var createdKey = registrar.RegisterMessageFactory<CreatedObjectMessage>(
                () => new CreatedObjectMessage());

            var deletedKey = registrar.RegisterMessageFactory<DeletedObjectMessage>(
                () => new DeletedObjectMessage());

            var handler = new Action<object>(
                o =>
                {
                    var messageType = (o is CreatedObjectMessage) ? "creation" : "deletion";
                    Console.WriteLine($"Received {messageType} message");

                    messageList.Add((Message)o);
                }
            );
            registrar.RegisterMessageHandler<CreatedObjectMessage>(handler);
            registrar.RegisterMessageHandler<DeletedObjectMessage>(handler);

            var listenIpAddress = NetworkUtility.GetConnectedIpAddresses(false).First().ToString();

            Console.WriteLine($"Using address {listenIpAddress}");

            MessageService messageService = new MessageService(registrar, listenIpAddress);
            messageService.Open();

            Console.WriteLine("Opened message channel, waiting for messages");
            Console.WriteLine(
                "Press R to replay all messages, M to consume & play oldest, N to replay oldest, X to quit");

            while (true)
            {
                var key = Console.ReadKey();

                if (key.Key == ConsoleKey.X)
                {
                    break;
                }
                if (key.Key == ConsoleKey.R)
                {
                    if (messageList.Count == 0)
                    {
                        Console.WriteLine("No messages to replay");
                    }
                    else
                    {
                        Console.WriteLine($"Replaying {messageList.Count} messages");
                        var msgs = messageList;
                        messageList = new List<Message>();

                        foreach (var msg in msgs)
                        {
                            if (msg.GetType() == typeof(CreatedObjectMessage))
                            {
                                messageService.Send((CreatedObjectMessage)msg, null);
                            }
                            else
                            {
                                messageService.Send((DeletedObjectMessage)msg, null);
                            }
                        }
                    }
                }
                if ((key.Key == ConsoleKey.M) || (key.Key == ConsoleKey.N))
                {
                    if (messageList.Count > 0)
                    {
                        Console.WriteLine("Replaying the oldest message received");

                        var msg = messageList[0];

                        if (key.Key == ConsoleKey.M)
                        {
                            messageList.RemoveAt(0);
                        }
                        if (msg is CreatedObjectMessage)
                        {
                            messageService.Send((CreatedObjectMessage)msg);
                        }
                        else
                        {
                            messageService.Send((DeletedObjectMessage)msg);
                        }
                    }
                    else
                    {
                        Console.WriteLine("No messages in list");
                    }
                }
            }
            messageService.Close();
            messageService.Dispose();

            Console.WriteLine("Done");
        }
    }
}
