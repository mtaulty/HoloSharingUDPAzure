namespace UwpApp
{
    using BroadcastMessaging;
    using SharedTestAppCode;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();
            this.remoteAddresses = new List<Guid>();
            this.Loaded += OnLoaded;
        }
        public string IPAddress
        {
            get
            {
                return (this.ipAddress);
            }
            set
            {
                if (value != this.ipAddress)
                {
                    this.ipAddress = value;
                    this.FirePropertyChanged();
                }
            }
        }
        public Guid MessageId
        {
            get
            {
                return (this.messageId);
            }
            set
            {
                if (value != this.messageId)
                {
                    this.messageId = value;
                    this.FirePropertyChanged();
                }
            }
        }
        public int SentCount
        {
            get
            {
                return (this.sentCount);
            }
            set
            {
                if (value != this.sentCount)
                {
                    this.sentCount = value;
                    this.FirePropertyChanged();
                }
            }
        }
        public int ReceivedCount
        {
            get
            {
                return (this.receivedCount);
            }
            set
            {
                if (value != this.receivedCount)
                {
                    this.receivedCount = value;
                    this.FirePropertyChanged();
                }
            }
        }
        public int SourceCount
        {
            get
            {
                return (this.sourceCount);
            }
            set
            {
                if (value != this.sourceCount)
                {
                    this.sourceCount = value;
                    this.FirePropertyChanged();
                }
            }
        }
        async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var messageRegistrar = new MessageRegistrar();

            var key = messageRegistrar.RegisterMessageFactory<TestGuidMessage>(
                () => new TestGuidMessage());

            messageRegistrar.RegisterMessageHandler<TestGuidMessage>(key,
                async msg =>
                {
                    var testMessage = msg as TestGuidMessage;

                    await this.DispatchAsync(
                        () =>
                        {
                            if (!this.remoteAddresses.Contains(testMessage.Id))
                            {
                                this.remoteAddresses.Add(testMessage.Id);
                                this.SourceCount++;
                            }
                            this.ReceivedCount++;
                        }
                    );
                }
            );
            this.IPAddress = NetworkUtility.GetConnectedIpAddresses(false).First().ToString();

            this.MessageId = Guid.NewGuid();

            this.messageService = new MessageService(
                messageRegistrar, ipAddress.ToString());

            this.messageService.Open();
        }
        async Task DispatchAsync(Action action)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => action());
        }
        public void OnSendMessage()
        {
            messageService.Send(
                new TestGuidMessage()
                {
                    Id = this.MessageId
                },
                async sent =>
                {
                    await this.DispatchAsync(
                        () =>
                        {
                            this.SentCount++;
                        }
                    );
                }
            );
        }
        void FirePropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }
        Guid messageId;
        string ipAddress;
        int receivedCount;
        int sourceCount;
        int sentCount;
        MessageService messageService;
        List<Guid> remoteAddresses;
    }
}