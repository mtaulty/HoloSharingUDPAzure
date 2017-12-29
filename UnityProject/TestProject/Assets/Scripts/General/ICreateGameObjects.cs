using System;
using UnityEngine;

public interface ICreateGameObjects
{
    void CreateGameObject(string gameObjectSpecifier, Action<GameObject> callback);
}