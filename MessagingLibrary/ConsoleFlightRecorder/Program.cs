namespace ConsoleFlightRecorder
{
    using BroadcastMessaging;
    using SharedHolograms.Messages;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    class Program
    {
        class MessageTime
        {
            public Message Message;
            public TimeSpan Delay;
        }
        static void SendMessage(MessageService messageService, Message message)
        {
            // TODO: all this 'use of types' is getting ugly and especially when trying to
            // do something as simple as call the Send() method. Needs resolving.
            switch (message.GetType().Name)
            {
                case "CreatedObjectMessage":
                    messageService.Send<CreatedObjectMessage>(message as CreatedObjectMessage);
                    break;
                case "DeletedObjectMessage":
                    messageService.Send<DeletedObjectMessage>(message as DeletedObjectMessage);
                    break;
                case "TransformMessage":
                    messageService.Send<TransformMessage>(message as TransformMessage);
                    break;
                default:
                    break;
            }
        }

        static void Main(string[] args)
        {
            var registrar = new MessageRegistrar();
            var lastMessageArrivalTime = DateTime.Now;

            List<MessageTime> messageList = new List<MessageTime>();

            var createdKey = registrar.RegisterMessageFactory<CreatedObjectMessage>(
                () => new CreatedObjectMessage());

            var deletedKey = registrar.RegisterMessageFactory<DeletedObjectMessage>(
                () => new DeletedObjectMessage());

            var transformKey = registrar.RegisterMessageFactory<TransformMessage>(
                () => new TransformMessage());

            var handler = new Action<object>(
                o =>
                {
                    // Yuk, I need to fix this dependency on TYPEs as it's getting out of hand already :-)
                    Console.WriteLine($"Received {o.GetType().Name} message");

                    messageList.Add(
                        new MessageTime()
                        {
                            Message = o as Message,
                            Delay = DateTime.Now - lastMessageArrivalTime
                        }
                    );
                    lastMessageArrivalTime = DateTime.Now;
                }
            );
            registrar.RegisterMessageHandler<CreatedObjectMessage>(handler);
            registrar.RegisterMessageHandler<DeletedObjectMessage>(handler);
            registrar.RegisterMessageHandler<TransformMessage>(handler);

            var listenIpAddress = NetworkUtility.GetConnectedIpAddresses(false).First().ToString();

            Console.WriteLine($"Using address {listenIpAddress}");

            MessageService messageService = new MessageService(registrar, listenIpAddress);
            messageService.Open();

            Console.WriteLine("Opened message channel, waiting for messages");
            Console.WriteLine(
                "Press R to replay messages in list, S to replay with delay timings, M to consume & play oldest, N to replay oldest, X to quit");

            while (true)
            {
                var key = Console.ReadKey();

                if (key.Key == ConsoleKey.X)
                {
                    break;
                }
                if ((key.Key == ConsoleKey.R) || (key.Key == ConsoleKey.S))
                {
                    if (messageList.Count == 0)
                    {
                        Console.WriteLine("No messages to replay");
                    }
                    else
                    {
                        Console.WriteLine($"Replaying {messageList.Count} messages");
                        var msgs = messageList;
                        messageList = new List<MessageTime>();

                        foreach (var msg in msgs)
                        {
                            SendMessage(messageService, msg.Message);

                            if (key.Key == ConsoleKey.S)
                            {
                                Thread.Sleep(msg.Delay);
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
                        SendMessage(messageService, msg.Message);
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
