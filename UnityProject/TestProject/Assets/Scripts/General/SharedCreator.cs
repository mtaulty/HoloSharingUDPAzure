namespace SharedHolograms
{
    using BroadcastMessaging;
    using System;
    using UnityEngine;
    using SharedHolograms.Messages;
    using SharedHolograms.AzureBlobs;

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
            this.worldAnchorMap = new WorldAnchorMap();

            this.messageService = messageService;

            this.messageService.Registrar.RegisterMessageHandler<CreatedObjectMessage>(
                this.OnObjectCreatedRemotely);

            this.messageService.Registrar.RegisterMessageHandler<DeletedObjectMessage>(
                this.OnObjectDeletedRemotely);

            this.syncDetails = syncDetails;

            if (this.syncDetails.SynchronizeTransforms)
            {
                this.messageService.Registrar.RegisterMessageHandler<TransformMessage>(
                    this.OnObjectTransformedRemotely);
            }
            this.storageDetails = storageDetails;

            this.GameObjectCreator = new PrimitiveGameObjectCreator();
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
            CreatedObjectMessage.Send(
                this.messageService,
                gameObjectType,
                gameObject,
                worldAnchorParent);

            if (callback != null)
            {
                callback(gameObject);
            }
        }
        public void Delete(GameObject gameObject)
        {
            // Tell the other devices about the deletion.
            this.messageService.Send(
                new DeletedObjectMessage()
                {
                    ObjectId = gameObject.name
                }
            );
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
                if (gameObject != null)
                {
                    GameObject.Destroy(gameObject);
                }
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
        MessageService messageService;
        WorldAnchorMap worldAnchorMap;
        AzureStorageDetails storageDetails;
        SynchronizationDetails syncDetails;
    }
}