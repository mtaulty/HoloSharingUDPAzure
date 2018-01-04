namespace SharedHolograms
{
    using BroadcastMessaging;
    using System;
    using UnityEngine;
    using SharedHolograms.Messages;
    using SharedHolograms.AzureBlobs;
    using System.Collections.Generic;
    using System.Linq;

#if !UNITY_EDITOR
    using UnityEngine.XR.WSA;
    using SharedHolograms.AzureBlobs;
#endif

    public class SharedCreator
    {
        public SharedCreator(
            MessageService messageService,
            AzureStorageDetails storageDetails,
            SynchronizationDetails syncDetails)
        {
            this.deviceId = new Guid();
            this.startTime = DateTime.Now;
            this.sharedObjectsInSceneMessages = new List<CreatedObjectMessage>();
            this.worldAnchorMap = new WorldAnchorMap();
            this.messageService = messageService;
            this.syncDetails = syncDetails;
            this.storageDetails = storageDetails;
            this.GameObjectCreator = new PrimitiveGameObjectCreator();

            this.RegisterMessageHandlers();
            this.AnnounceNewDeviceToNetwork();
        }

        public ICreateGameObjects GameObjectCreator
        {
            get;
            set;
        }
        public void Create(
            string gameObjectType,
            Vector3 position,
            Vector3 forward,
            Vector3 scale,
            Action<GameObject> callback)
        {
            CreateGameObject(
                gameObjectType,
                position,
                forward,
                scale,
                null,
                gameObject =>
                {
                    this.HandleLocallyCreateGameObject(gameObject, gameObjectType, callback);
                }
            );
        }
        void RegisterMessageHandlers()
        {
            // Register the message factories (this got laborious, I should change the API).
            // Also, these messages (and this class) have become a bit of a state machine
            // in their own right and I should probably fix that too.
            this.messageService.Registrar.RegisterMessageFactory<CreatedObjectMessage>(
                () => new CreatedObjectMessage());
            this.messageService.Registrar.RegisterMessageFactory<DeletedObjectMessage>(
                () => new DeletedObjectMessage());
            this.messageService.Registrar.RegisterMessageFactory<TransformMessage>(
                () => new TransformMessage());
            this.messageService.Registrar.RegisterMessageFactory<NewDeviceAnnouncementMessage>(
                () => new NewDeviceAnnouncementMessage());
            this.messageService.Registrar.RegisterMessageFactory<ExistingDeviceMessage>(
                () => new ExistingDeviceMessage());
            this.messageService.Registrar.RegisterMessageFactory<SceneRequestMessage>(
                () => new SceneRequestMessage());
            this.messageService.Registrar.RegisterMessageFactory<SceneResponseObjectMessage>(
                () => new SceneResponseObjectMessage());

            // And the handlers.
            this.messageService.Registrar.RegisterMessageHandler<CreatedObjectMessage>(
                this.OnObjectCreatedRemotely);

            this.messageService.Registrar.RegisterMessageHandler<DeletedObjectMessage>(
                this.OnObjectDeletedRemotely);

            this.messageService.Registrar.RegisterMessageHandler<NewDeviceAnnouncementMessage>(
                this.OnNewDeviceAnnouncedToNetwork);

            this.messageService.Registrar.RegisterMessageHandler<ExistingDeviceMessage>(
                this.OnExistingDeviceAnnounced);

            this.messageService.Registrar.RegisterMessageHandler<SceneRequestMessage>(
                this.OnNewRemoteDeviceRequestsScene);

            this.messageService.Registrar.RegisterMessageHandler<SceneResponseObjectMessage>(
                this.OnNewSceneResponse);

            if (this.syncDetails.SynchronizeTransforms)
            {
                this.messageService.Registrar.RegisterMessageHandler<TransformMessage>(
                    this.OnObjectTransformedRemotely);
            }
        }
        void HandleLocallyCreateGameObject(GameObject gameObject, string gameObjectType, Action<GameObject> callback)
        {
            // Do we have a world anchor for this position already?
            GameObject worldAnchorParent = null;

            bool addedAnchor = this.worldAnchorMap.GetOrAddWorldAnchorForPosition(
                gameObject.transform.position,
                gameObject.transform.forward,
                out worldAnchorParent);

            // parent the GameObject off its anchor without moving it, the assumption
            // being that if didn't already have an anchor then we just created it
            // at the same place as the object itself and so once we reparent we
            // are hoping that object and the anchor are identical in placement
            // (making for a LocalPosition of 0).
            gameObject.transform.SetParent(worldAnchorParent.transform, true);

#if !UNITY_EDITOR
            // We only do this work with world anchors if we aren't in the editor
            // as I don't think a WorldAnchor in the editor will ever say that it
            // isLocated so it'll break our logic.
            if (addedAnchor)
            {
                // Now export that to get a bunch of bytes for it...
                WorldAnchorImportExportHelper.ExportWorldAnchorFromGameObject(
                    worldAnchorParent,
                    bits =>
                    {
                        if (bits != null)
                        {
                            // We now need to send those bits off to the cloud...
                            AzureBlobStorageHelper.UploadWorldAnchorBlob(
                                this.storageDetails,
                                worldAnchorParent.name,
                                bits,
                                (worked, bytes) =>
                                {
                                    this.ConfigureTransformSynchronizer(gameObject);
                                    this.SendCreatedObjectMessage(
                                        gameObjectType, gameObject, worldAnchorParent, callback);
                                }
                            );
                        }
                        else
                        {
                            // TODO: figure what we do here.
                        }
                    }
                );
            }
            else
            {
                this.ConfigureTransformSynchronizer(gameObject);

                // Send a message to the world telling them about the new object.
                this.SendCreatedObjectMessage(
                    gameObjectType, gameObject, worldAnchorParent, callback);
            }
#else
            // Send a message to the world telling them about the new object - it's
            // important to note that we pass whether we newly created an anchor or
            // not as that impacts the message that we send.
            this.ConfigureTransformSynchronizer(gameObject);

            this.SendCreatedObjectMessage(
                gameObjectType, gameObject, worldAnchorParent, callback);
#endif
        }
        void ConfigureTransformSynchronizer(GameObject gameObject)
        {
            if (this.syncDetails.SynchronizeTransforms)
            {
                var synchronizer = gameObject.AddComponent<TransformSynchronizer>();

                // TODO: Come up with a better way of doing this, it's ugly.
                synchronizer.Initialise(
                    this.syncDetails.SynchronizationIntervalSeconds,
                    message =>
                    {
                        this.messageService.Send<TransformMessage>(message);
                    }
                );
            }
        }
        void SendCreatedObjectMessage(
            string gameObjectType,
            GameObject gameObject,
            GameObject worldAnchorParent,
            Action<GameObject> callback)
        {
            var message = CreatedObjectMessage.Send(
                this.messageService,
                gameObjectType,
                gameObject,
                worldAnchorParent);

            this.sharedObjectsInSceneMessages.Add(message);

            if (callback != null)
            {
                callback(gameObject);
            }
        }
        public void Delete(GameObject gameObject)
        {
            this.DeleteInternal(gameObject, true);
        }
        void DeleteInternal(GameObject gameObject, bool sendMessage)
        {
            if (sendMessage)
            {
                // Tell the other devices about the deletion.
                this.messageService.Send(
                    new DeletedObjectMessage()
                    {
                        ObjectId = gameObject.name
                    }
                );
            }

            this.sharedObjectsInSceneMessages.Remove(
                this.sharedObjectsInSceneMessages.Single(o => o.ObjectId == gameObject.name));

            GameObject.Destroy(gameObject);
        }
        void OnObjectCreatedRemotely(object obj)
        {
            var message = obj as CreatedObjectMessage;

            if (message != null)
            {
                // Make the object locally first - note the effective dummy values for
                // position and forward.
                CreateGameObject(
                    message.ObjectType,
                    Vector3.zero,
                    Vector3.forward,
                    message.LocalScale,
                    message.ObjectId,
                    gameObject =>
                    {
                        this.HandleRemotelyCreatedGameObject(gameObject, message);
                    }
                );
            }
        }
        void HandleRemotelyCreatedGameObject(GameObject gameObject, CreatedObjectMessage message)
        {
            bool newAnchor = false;

            // Do we already know the anchor that the GameObject is associated with?
            var anchorObject = this.worldAnchorMap.GetById(message.ParentAnchorId);

            newAnchor = (anchorObject == null);

            // If we don't have one...
            if (newAnchor)
            {
                // Make one but it's not anchored at this point
                anchorObject = this.worldAnchorMap.AddAnchorWithExistingIdAtOrigin(
                    message.ParentAnchorId);
            }
            // Parent the object off the anchor object, hoping that this
            // will be ok even if we later go on to import the anchor.
            gameObject.transform.SetParent(anchorObject.transform, false);
            gameObject.transform.localPosition = message.LocalPosition;
            gameObject.transform.localRotation = message.LocalRotation;

            this.sharedObjectsInSceneMessages.Add(message);

#if !UNITY_EDITOR
            if (newAnchor)
            {
                // We need to go off to the cloud and download that anchor.
                AzureBlobStorageHelper.DownloadWorldAnchorBlob(
                    this.storageDetails, 
                    message.ParentAnchorId,
                    (worked, bits) =>
                    {
                        if (worked)
                        {
                            // Having got the bits, we need to import them onto the
                            // game object which means that this object is likely
                            // to change its position and orientation.
                            WorldAnchorImportExportHelper.ImportWorldAnchorToGameObject(
                                anchorObject,
                                bits,
                                null);
                        }                               
                    }
                );
            }
#endif
            this.ConfigureTransformSynchronizer(gameObject);
        }
        void OnObjectDeletedRemotely(object obj)
        {
            var msg = obj as DeletedObjectMessage;

            if (msg != null)
            {
                var gameObject = GameObject.Find(msg.ObjectId);

                this.DeleteInternal(gameObject, false);
            }
        }
        void CreateGameObject(
            string gameObjectType,
            Vector3 position,
            Vector3 forward,
            Vector3 scale,
            string name,
            Action<GameObject> callback)
        {
            this.GameObjectCreator.CreateGameObject(gameObjectType,
                gameObject =>
                {
                    gameObject.transform.position = position;
                    gameObject.transform.forward = forward;
                    gameObject.transform.localScale = scale;
                    gameObject.name = name ?? Guid.NewGuid().ToString();
                    callback(gameObject);
                }
            );
        }
        void OnObjectTransformedRemotely(object obj)
        {
            var message = obj as TransformMessage;

            if (message != null)
            {
                // Should we keep our own map of IDs<->GameObject or should we just
                // let Unity look it up for us?
                var gameObject = GameObject.Find(message.ObjectId);

                if (gameObject != null)
                {
                    var synchronizer = gameObject.GetComponent<TransformSynchronizer>();
                    if (synchronizer != null)
                    {
                        synchronizer.UpdateTransforms(
                            message.LocalPosition,
                            message.LocalRotation,
                            message.LocalScale);
                    }
                }

            }
        }
        void AnnounceNewDeviceToNetwork()
        {
            // Say 'hello' to the network giving others a stable id for ourselves.
            this.messageService.Send<NewDeviceAnnouncementMessage>(
                new NewDeviceAnnouncementMessage()
                {
                    DeviceId = this.deviceId
                }
            );
        }
        void OnNewDeviceAnnouncedToNetwork(object obj)
        {
            // If this device has content then we can send out an announcement
            // advertising that we have content for the newly announced device.
            if (this.sharedObjectsInSceneMessages.Count > 0)
            {
                // Ok, we have something that we can share with other devices so
                // let them know about it.
                this.messageService.Send<ExistingDeviceMessage>(
                    new ExistingDeviceMessage()
                    {
                        DeviceId = this.deviceId
                    }
                );
            }
        }
        void OnExistingDeviceAnnounced(object obj)
        {
            // Are we waiting for one of these announcements? Possibly, if we haven't been
            // running for very long and we haven't any content of our own then we probably
            // multicasted to find one and got one so we should take a look at the first
            // one that we find.
            if ((this.Uptime.TotalSeconds <= NEW_UPTIME_SECS) && !this.existingDeviceAcknowledged)
            {
                this.existingDeviceAcknowledged = true;

                var existingDeviceMsg = obj as ExistingDeviceMessage;

                // Ok, we're a new device. We've advertised. Another device has responded that
                // it has content so let's ask that device for that content.
                this.messageService.Send<SceneRequestMessage>(
                    new SceneRequestMessage()
                    {
                        RequestingDeviceId = this.deviceId,
                        ProvidingDeviceId = existingDeviceMsg.DeviceId
                    }
                );
            }
        }
        void OnNewRemoteDeviceRequestsScene(object obj)
        {
            var sceneRequestMsg = obj as SceneRequestMessage;

            // We only process the request if it was sent to us to avoid lots of these messages
            // getting processed and extra messages then being sent.
            if ((sceneRequestMsg != null) && (this.deviceId == sceneRequestMsg.ProvidingDeviceId))
            {
                foreach (var sharedObjectMsg in this.sharedObjectsInSceneMessages)
                {
                    var gameObject = GameObject.Find(sharedObjectMsg.ObjectId);

                    // bring these up to date...
                    sharedObjectMsg.LocalPosition = gameObject.transform.localPosition;
                    sharedObjectMsg.LocalRotation = gameObject.transform.localRotation;
                    sharedObjectMsg.LocalScale = gameObject.transform.localScale;

                    // Wrap it up into another message...
                    var message = SceneResponseObjectMessage.FromCreateObjectMessage(sharedObjectMsg);

                    // Address it...
                    message.DestinationDeviceId = sceneRequestMsg.RequestingDeviceId;

                    // Send it...
                    this.messageService.Send<SceneResponseObjectMessage>(message);
                }
            }
        }
        void OnNewSceneResponse(object obj)
        {
            var message = obj as SceneResponseObjectMessage;

            if ((message != null) && (message.DestinationDeviceId == this.deviceId))
            {
                // Now treat this message as though it arrived remotely in 'real time' from
                // another device.
                this.OnObjectCreatedRemotely(message);
            }
        }
        TimeSpan Uptime
        {
            get
            {
                return (DateTime.Now - this.startTime);
            }
        }
        bool existingDeviceAcknowledged;
        List<CreatedObjectMessage> sharedObjectsInSceneMessages;
        MessageService messageService;
        WorldAnchorMap worldAnchorMap;
        AzureStorageDetails storageDetails;
        SynchronizationDetails syncDetails;
        Guid deviceId;
        DateTime startTime;
        const int NEW_UPTIME_SECS = 15;
    }
}