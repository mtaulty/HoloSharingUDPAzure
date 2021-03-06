﻿namespace BroadcastMessaging
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;

    public class MessageService : IDisposable
    {
        // Note: 239.0.0.0 is the start of the UDP multicast addresses reserved for
        // private use.
        // Note: 49152 is the result I get out of executing;
        //      netsh int ipv4 show dynamicport udp
        // on Windows 10.
        public MessageService(
            MessageRegistrar messageRegistrar,
            string localNetworkAddress,
            string multicastAddress = "239.0.0.0",
            int multicastPort = 49152)
        {
            //if (messageRegistrar == null)
            //{
            //    throw new ArgumentNullException("MessageRegistrar is null");
            //}
            this.messageRegistrar = messageRegistrar;
            this.localNetworkAddress = IPAddress.Parse(localNetworkAddress);
            this.multicastEndpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), multicastPort);
        }
        public MessageRegistrar Registrar
        {
            get
            {
                return (this.messageRegistrar);
            }
        }
        public void Open()
        {
            this.CheckOpen(false);

            this.udpClient = new UdpClient(
                new IPEndPoint(this.localNetworkAddress, this.multicastEndpoint.Port));

            this.udpClient.MulticastLoopback = false;

            this.udpClient.JoinMulticastGroup(this.multicastEndpoint.Address,
                this.localNetworkAddress);

            this.ReceiveInternal();
        }
        public void Close()
        {
            this.CheckOpen();
#if WINDOWS_UWP
            this.udpClient.Dispose();
#else
            this.udpClient.Close();
#endif
            this.udpClient = null;
        }
        public void Send<T>(T message, Action<bool> callback = null) where T : Message
        {
            var bits = this.Serialize<T>(message);

            this.SendInternalAsync(bits, callback);
        }
        byte[] Serialize<T>(T message) where T : Message
        {
            var stream = new MemoryStream();

            var writer = new BinaryWriter(stream);

            try
            {
                writer.Write(MessageRegistrar.KeyFromMessageType<T>());
                message.Save(writer);
                writer.Flush();

                if (stream.Length > Constants.MAX_UDP_SIZE)
                {
                    throw new ArgumentException("Message size exceeded maximum length");
                }
            }
            finally
            {
#if WINDOWS_UWP
                writer.Dispose();
#else
                writer.Close();
#endif
            }
            return (stream.ToArray());
        }
        public void Dispose()
        {
            this.Dispose(true);
        }
        void Dispose(bool disposing)
        {
            if (disposing && (this.udpClient != null))
            {
                this.Close();
                GC.SuppressFinalize(this);
            }
        }
        void CheckOpen(bool open = true)
        {
            if ((this.udpClient == null) && open)
            {
                throw new InvalidOperationException("Not open");
            }
            if ((this.udpClient != null) && !open)
            {
                throw new InvalidOperationException("Already open");
            }
        }
        void DispatchMessage(byte[] bits)
        {
            var stream = new MemoryStream(bits);
            Message message = null;

            using (var reader = new BinaryReader(stream))
            {
                var messageTypeKey = reader.ReadString();
                message = this.messageRegistrar.CreateMessage(messageTypeKey);
                message.Load(reader);

                this.messageRegistrar.InvokeHandlers(message);
            }
        }
#if WINDOWS_UWP
        async void SendInternalAsync(byte[] bits, Action<bool> callback)
        {
            bool sent = false;

            try
            {
                var sendCount = await this.udpClient.SendAsync(bits, bits.Length, this.multicastEndpoint);
                Debug.Assert(sendCount == bits.Length);
                sent = true;
            }
            catch (ObjectDisposedException)
            {

            }
            catch (SocketException)
            {
            }
            if (callback != null)
            {
                callback(sent);
            }
        }
        async void ReceiveInternal()
        {
            bool failed = false;

            while (!failed)
            {
                try
                {
                    var result = await this.udpClient.ReceiveAsync();

                    if (result.Buffer != null)
                    {
                        this.DispatchMessage(result.Buffer);
                    }
                }
                catch (SocketException) // TODO: verify that this is the right exception
                {
                    failed = true;
                }
            }
        }
#else
        void SendInternalAsync(byte[] bits, Action<bool> callback)
        {
            this.udpClient.BeginSend(
                bits,
                bits.Length,
                this.multicastEndpoint,
                iar =>
                {
                    bool sent = false;

                    try
                    {
                        var sendCount = this.udpClient.EndSend(iar);
                        Debug.Assert(sendCount == bits.Length);
                        sent = true;
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (SocketException)
                    {
                    }
                    if (callback != null)
                    {
                        callback(sent);
                    }
                },
                null
            );
        }
        void ReceiveInternal()
        {
            this.udpClient.BeginReceive(
                iar =>
                {
                    try
                    {
                        IPEndPoint remote = new IPEndPoint(0,0);

                        byte[] bits = this.udpClient.EndReceive(iar, ref remote);

                        if (bits != null)
                        {
                            this.DispatchMessage(bits);
                        }
                        this.ReceiveInternal();
                    }
                    catch (ObjectDisposedException)
                    {

                    }
                    catch (SocketException) // TODO: verify right exception
                    {
                    }
                },
                null
            );
        }
#endif
        UdpClient udpClient;
        MessageRegistrar messageRegistrar;
        IPEndPoint multicastEndpoint;
        IPAddress localNetworkAddress;
    }
}
