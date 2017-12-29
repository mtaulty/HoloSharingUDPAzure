using SharedHolograms.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformSynchronizer : MonoBehaviour
{
    public TransformSynchronizer()
    {
        this.message = new TransformMessage();
    }
    void Start()
    {
        this.message.ObjectId = this.gameObject.name;
    }
    public void Initialise(
        float intervalSeconds, Action<TransformMessage> messageSender)
    {
        this.messageSender = messageSender;
        this.UpdateSnapshot();
        this.InvokeRepeating("OnTick", intervalSeconds, intervalSeconds); 
    }
    public void UpdateTransforms(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        this.gameObject.transform.localPosition = localPosition;
        this.gameObject.transform.localRotation = localRotation;
        this.gameObject.transform.localScale = localScale;
        this.UpdateSnapshot();
    }
    void UpdateSnapshot()
    {
        this.message.LocalPosition = this.gameObject.transform.localPosition;
        this.message.LocalRotation = this.gameObject.transform.localRotation;
        this.message.LocalScale = this.gameObject.transform.localScale;
    }
    bool IsModifiedFromSnapshot
    {
        get
        {
            return (
                (this.message.LocalPosition != this.gameObject.transform.localPosition) ||
                (this.message.LocalRotation != this.gameObject.transform.localRotation) ||
                (this.message.LocalScale != this.gameObject.transform.localScale));
        }
    }
    void SendUpdateMessage()
    {
        this.messageSender(this.message);
    }
    void OnTick()
    {
        var transform = this.gameObject.transform;

        if (this.IsModifiedFromSnapshot)
        {
            this.UpdateSnapshot();
            this.SendUpdateMessage();
        }
    }
    Action<TransformMessage> messageSender;
    TransformMessage message;
}