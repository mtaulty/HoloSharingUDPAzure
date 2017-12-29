using System;
using UnityEngine;

public class PrimitiveGameObjectCreator : ICreateGameObjects
{
    public void CreateGameObject(string gameObjectSpecifier, Action<GameObject> callback)
    {
        GameObject resolvedHologram = null;

        if (!string.IsNullOrEmpty(gameObjectSpecifier))
        {
            var primitiveType = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), gameObjectSpecifier);

            resolvedHologram = GameObject.CreatePrimitive(primitiveType);
        }
        callback(resolvedHologram);
    }
}