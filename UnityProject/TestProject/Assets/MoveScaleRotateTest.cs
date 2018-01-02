using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveScaleRotateTest : MonoBehaviour
{
    private void Start()
    {
        initialScale = this.gameObject.transform.localScale.x;
        initialX = this.gameObject.transform.localPosition.x;
    }
    void Update()
    {
        // Turn the object slowly.
        this.gameObject.transform.Rotate(
            Vector3.up,
            360.0f * Time.deltaTime / 10.0f,
            Space.Self);

        var repeatingFactor = Mathf.Abs(Mathf.Sin(Time.frameCount / 120.0f));

        var scaleXYZ = Mathf.Lerp(
                initialScale * 0.5f,
                initialScale * 1.5f,
                repeatingFactor);

        this.gameObject.transform.localScale = new Vector3(scaleXYZ, scaleXYZ, scaleXYZ);

        var translationX = Mathf.Lerp(-0.5f, 0.5f, repeatingFactor);

        this.gameObject.transform.localPosition = 
            new Vector3(
                initialX + translationX, 
                this.gameObject.transform.localPosition.y,
                this.gameObject.transform.localPosition.z);
    }
    float initialX = 0.0f;
    float initialScale = 0.0f;
    const float MAX_SCALE = 1.5f;
    const float MIN_SCALE = 0.75f;
}