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
            var deviceId = Guid.NewGuid();

            List<MessageTime> messageList = new List<MessageTime>();
            List<SceneResponseObjectMessage> sceneResponses = new List<SceneResponseObjectMessage>();

            var createdKey = registrar.RegisterMessageFactory<CreatedObjectMessage>(
                () => new CreatedObjectMessage());

            var deletedKey = registrar.RegisterMessageFactory<DeletedObjectMessage>(
                () => new DeletedObjectMessage());

            var transformKey = registrar.RegisterMessageFactory<TransformMessage>(
                () => new TransformMessage());

            registrar.RegisterMessageFactory<NewDeviceAnnouncementMessage>(
                () => new NewDeviceAnnouncementMessage());

            registrar.RegisterMessageFactory<ExistingDeviceMessage>(
                () => new ExistingDeviceMessage());

            registrar.RegisterMessageFactory<SceneRequestMessage>(
                () => new SceneRequestMessage());

            registrar.RegisterMessageFactory<SceneResponseObjectMessage>(
                () => new SceneResponseObjectMessage());

            // CREATED OBJECT, DELETED OBJECT, TRANSFORM messages.
            // Generally, we just write messages to the console and note their delivery
            // time while adding them to a list.
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

            // Set up the message service.
            var listenIpAddress = NetworkUtility.GetConnectedIpAddresses(false).First().ToString();

            Console.WriteLine($"Using address {listenIpAddress}");

            MessageService messageService = new MessageService(registrar, listenIpAddress);

            // NEW DEVICE ANNOUNCEMENT MESSAGES
            // We respond to 'new device' announcements by offering our services
            var newDeviceHandler = new Action<object>(
                o =>
                {
                    var msg = (NewDeviceAnnouncementMessage)o;

                    Console.WriteLine("Received a new device announcement, responding with our ID");
                    messageService.Send<ExistingDeviceMessage>(
                        new ExistingDeviceMessage()
                        {
                            DeviceId = deviceId
                        }
                    );
                }
            );
            registrar.RegisterMessageHandler<NewDeviceAnnouncementMessage>(newDeviceHandler);

            // EXISTING DEVICE MESSAGES...
            var existingDeviceHandler = new Action<object>(
                o =>
                {
                    var msg = (ExistingDeviceMessage)o;

                    Console.WriteLine("Got an existing device response, sending a scene request message");

                    // Having been told of an existing device, let's then ask it
                    // for a scene update.
                    messageService.Send<SceneRequestMessage>(
                        new SceneRequestMessage()
                        {
                            RequestingDeviceId = deviceId,
                            ProvidingDeviceId = msg.DeviceId
                        }
                    );
                }
            );
            registrar.RegisterMessageHandler<ExistingDeviceMessage>(existingDeviceHandler);

            // SCENCE REQUEST MESSAGES 
            // We respond to 'scene request' messages by offering our services
            var sceneRequestHandler = new Action<object>(
                o =>
                {
                    var msg = (SceneRequestMessage)o;

                    if (msg.ProvidingDeviceId == deviceId)
                    {
                        Console.WriteLine("Recieved a scene request message");
                        Console.WriteLine($"Sending {sceneResponses.Count} messages in response");
                        foreach (var responseMessage in sceneResponses)
                        {
                            // Make sure the message is updated to reflect the device that asked for it.
                            responseMessage.DestinationDeviceId = msg.RequestingDeviceId;
                            messageService.Send<SceneResponseObjectMessage>(responseMessage);
                        }
                    }
                }
            );
            registrar.RegisterMessageHandler<SceneRequestMessage>(sceneRequestHandler);

            // SCENE RESPONSE OBJECT MESSAGES
            // We respond to 'scene responses' simply by storing those messages onto our 
            // list and then we can play them back if someone sends us a scene request
            // message. We act like a 'mirror' for those messages.
            var sceneResponseHandler = new Action<object>(
                o =>
                {
                    var msg = (SceneResponseObjectMessage)o;

                    Console.WriteLine("Got a scene response message...adding to list...");

                    if (msg.DestinationDeviceId == deviceId)
                    {
                        sceneResponses.Add(msg); 
                    }
                }
            );
            registrar.RegisterMessageHandler<SceneResponseObjectMessage>(sceneResponseHandler);

            messageService.Open();

            Console.WriteLine("Opened message channel, waiting for messages");
            Console.WriteLine("Press R to replay messages in list without delay timings");
            Console.WriteLine("S to replay with delay timings");
            Console.WriteLine("M to consume & play oldest message in list");
            Console.WriteLine("N to replay oldest message in list without consuming");
            Console.WriteLine("D to send a 'new device announcement' message...");
            Console.WriteLine("and X to quit");

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

                if (key.Key == ConsoleKey.D)
                {
                    // New device scenario. We send out a "we are a new device message" and
                    // expect to get back an offer from an existing device.
                    var message = new NewDeviceAnnouncementMessage()
                    {
                        DeviceId = deviceId
                    };
                    Console.WriteLine("Sending new device announcement message...(waiting for response)");
                    messageService.Send<NewDeviceAnnouncementMessage>(message);
                }
            }
            messageService.Close();
            messageService.Dispose();

            Console.WriteLine("Done");
        }
    }
}
