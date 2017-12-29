namespace SharedHolograms
{
    using BroadcastMessaging;
    using SharedHolograms.AzureBlobs;
    using SharedHolograms.Messages;
    using System;
    using System.Linq;
    using UnityEngine;

    [RequireComponent(typeof(CoRoutineRunner))]
    public class SharedHologramsController : MonoBehaviour
    {
        public AzureStorageDetails StorageDetails;

        public SharedHologramsController()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Single instance expected for SharedHologramsController");
            }
            Instance = this;
        }
        public void Start()
        {
            var registrar = new MessageRegistrar();

            // Register the message types that we know about.
            registrar.RegisterMessageFactory<CreatedObjectMessage>(
                () => new CreatedObjectMessage());
            registrar.RegisterMessageFactory<DeletedObjectMessage>(
                () => new DeletedObjectMessage());

            // For the moment, I'm going to let this code try and figure out 'the right thing'
            // but we could surface these parameters to allow for more tweaking
            var localNetworkAddress = NetworkUtility.GetConnectedIpAddresses(false).FirstOrDefault();

            if (localNetworkAddress != null)
            {
                this.messageService = new MessageService(registrar, localNetworkAddress);
                this.messageService.Open();
                this.sharedCreator = new SharedCreator(
                    this.messageService, 
                    this.StorageDetails);
            }
        }
        public SharedCreator Creator
        {
            get
            {
                return (this.sharedCreator);
            }
        }
        public static SharedHologramsController Instance
        {
            get;
            private set;
        }
        ICreateGameObjects hologramResolver;
        SharedCreator sharedCreator;
        MessageService messageService;
    }
}