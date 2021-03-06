﻿namespace SharedHolograms
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

        public SynchronizationDetails SynchronizationDetails;

        public event EventHandler<SceneReadyEventArgs> SceneReady;

        public SharedHologramsController()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Single instance expected for SharedHologramsController");
            }
            Instance = this;
        }
        public void Awake()
        {
            var registrar = new MessageRegistrar();

            // For the moment, I'm going to let this code try and figure out 'the right thing'
            // but we could surface these parameters to allow for more tweaking
            var localNetworkAddress = NetworkUtility.GetConnectedIpAddresses(false).FirstOrDefault();

            if (localNetworkAddress != null)
            {
                this.messageService = new MessageService(registrar, localNetworkAddress);
                this.messageService.Open();
                this.sharedCreator = new SharedCreator(
                    this.messageService,
                    this.StorageDetails,
                    this.SynchronizationDetails);
            }
        }
        void Update()
        {
            SceneReadyStatus sceneStatus = SceneReadyStatus.Waiting;

            if (!this.hasFiredSceneReady && 
                ((sceneStatus = this.sharedCreator.SceneStatus) != SceneReadyStatus.Waiting))
            {
                this.hasFiredSceneReady = true;
                if (this.SceneReady != null)
                {
                    this.SceneReady(this, new SceneReadyEventArgs(sceneStatus));
                }
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
        bool hasFiredSceneReady;
        SharedCreator sharedCreator;
        MessageService messageService;
    }
}