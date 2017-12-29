using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    void Update()
    {
        this.gameObject.transform.Rotate(
            Vector3.up,
            360.0f * Time.deltaTime / 10.0f,
            Space.Self);
    }
}
