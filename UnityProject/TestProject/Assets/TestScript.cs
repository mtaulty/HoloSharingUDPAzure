using System;
using SharedHolograms;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

public class TestScript : MonoBehaviour
{
    void Start()
    {
        this.recognizer = new GestureRecognizer();
        this.recognizer.SetRecognizableGestures(GestureSettings.Tap);
        this.recognizer.Tapped += OnTapped;
        this.recognizer.StartCapturingGestures();
    }
    void OnTapped(TappedEventArgs obj)
    {
        // If we are staring at a cube, delete it. Otherwise, make a new one.
        if (this.lastHitCube == null)
        {
            this.CreateSharedCube();
        }
        else if (this.lastHitCube.GetComponent<MoveScaleRotateTest>() == null)
        {
            this.lastHitCube.AddComponent<MoveScaleRotateTest>();
        }
        else
        {
            this.DeleteSharedCube();
        }
    }
    void DeleteSharedCube()
    {
        SharedHologramsController.Instance.Creator.Delete(this.lastHitCube);
        this.lastHitCube = null;
    }
    void CreateSharedCube()
    {
        var forward = Camera.main.transform.forward;
        forward.Normalize();

        var position = Camera.main.transform.position + forward * 2.0f;

        // Note - there's potentially quite a long time here when the object has
        // been created but we're still doing network stuff so we'd need to really
        // make a UX that dealt with that which I haven't done here.
        SharedHologramsController.Instance.Creator.Create(
            "Cube",
            position,
            forward,
            new Vector3(0.1f, 0.1f, 0.1f),
            cube =>
            {
                ChangeMaterial(cube, this.GreenMaterial);
                cube.AddComponent<BoxCollider>();
            }
        );
    }
    void Update()
    {
        RaycastHit rayHitInfo;

        // Are we looking at a cube?
        if (Physics.Raycast(
            Camera.main.transform.position,
            Camera.main.transform.forward,
            out rayHitInfo,
            15.0f))
        {
            this.lastHitCube = rayHitInfo.collider.gameObject;
            ChangeMaterial(this.lastHitCube, this.RedMaterial);
        }
        else if (this.lastHitCube != null)
        {
            ChangeMaterial(this.lastHitCube, this.GreenMaterial);
            this.lastHitCube = null;
        }
    }
    static void ChangeMaterial(GameObject gameObject, Material material)
    {
        gameObject.GetComponent<Renderer>().material = material;
    }
    Material GreenMaterial
    {
        get
        {
            if (this.greenMaterial == null)
            {
                this.greenMaterial = new Material(Shader.Find("Legacy Shaders/Diffuse"));
                this.greenMaterial.color = Color.green;
            }
            return (this.greenMaterial);
        }
    }
    Material RedMaterial
    {
        get
        {
            if (this.redMaterial == null)
            {
                this.redMaterial = new Material(Shader.Find("Legacy Shaders/Diffuse"));
                this.redMaterial.color = Color.red;
            }
            return (this.redMaterial);
        }
    }
    Material greenMaterial;
    Material redMaterial;
    GestureRecognizer recognizer;
    GameObject lastHitCube;
}