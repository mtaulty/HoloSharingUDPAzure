
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
using UnityEngine.Windows.Speech;

public class Manipulations : MonoBehaviour
{
    public enum ManipulationMode
    {
        Rotate,
        Scale,
        Move
    }
    enum ManipulationState
    {
        Idle,
        Available,
        Active
    }

    [Serializable]
    public class ModeMaterial
    {
        public Material material;
        public ManipulationMode mode;
    }
    public ModeMaterial[] lineMaterials;

    public Manipulations()
    {
        this.manipulationState = ManipulationState.Idle;
    }
    private void Update()
    {
        RaycastHit raycastHit;

        var hit = Physics.Raycast(
            Camera.main.transform.position,
            Camera.main.transform.forward,
            out raycastHit,
            15.0f);

        var hitObject = hit ? raycastHit.collider.gameObject : null;
        var hitManipulations = hitObject != null ? hitObject.GetComponent<Manipulations>() : null;

        if (hitManipulations != focusedObject)
        {
            if (focusedObject != null)
            {
                focusedObject.Done();
                focusedObject.manipulationState = ManipulationState.Idle;
            }
            focusedObject = hitManipulations;

            if (focusedObject != null)
            {
                focusedObject.manipulationState = ManipulationState.Available;
            }
        }
    }
    void Start()
    {
        if (keywordRecognizer == null)
        {
            keywordRecognizer = new KeywordRecognizer(Enum.GetNames(typeof(ManipulationMode)));

            keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;

            keywordRecognizer.Start();
        }
        if (recognizer == null)
        {
            recognizer = new GestureRecognizer();
            recognizer.SetRecognizableGestures(GestureSettings.ManipulationTranslate);

            // NB: using these while deprecated because they matched up with code that I already
            // had which was based on the toolkit which also uses these.
            recognizer.ManipulationStartedEvent += OnManipulationStarted;
            recognizer.ManipulationUpdatedEvent += OnManipulationUpdated;
            recognizer.ManipulationCompletedEvent += OnManipulationCompleted;
            recognizer.ManipulationCanceledEvent += OnManipulationCanceled;
            recognizer.StartCapturingGestures();
        }
        if ((this.lineMaterials == null) || (this.lineMaterials.Length == 0))
        {
            this.lineMaterials = new ModeMaterial[]
            {
                new ModeMaterial()
                {
                  mode = ManipulationMode.Move,
                  material = Resources.Load<Material>("MoveMaterial")
                },
                new ModeMaterial()
                {
                  mode = ManipulationMode.Rotate,
                  material = Resources.Load<Material>("RotateMaterial")
                },
                new ModeMaterial()
                {
                  mode = ManipulationMode.Scale,
                  material = Resources.Load<Material>("ScaleMaterial")
                }
            };
        }
    }
    static void OnManipulationStarted(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headRay)
    {
        if (focusedObject != null)
        {
            if (focusedObject.manipulationState == ManipulationState.Available)
            {
                focusedObject.manipulationState = ManipulationState.Active;
                focusedObject.lastDelta = cumulativeDelta;
            }
        }
    }
    static void OnManipulationUpdated(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headRay)
    {
        if ((focusedObject != null) &&
            (focusedObject.manipulationState == ManipulationState.Active))
        {
            var delta = cumulativeDelta - focusedObject.lastDelta.Value;
            var xDelta = delta.x;
            var yDelta = delta.y;
            var zDelta = delta.z;
            var movement = new Vector3(xDelta, yDelta, zDelta);
            var magnitude =
              (xDelta > 0) ? movement.magnitude : 0 - movement.magnitude;

            switch (mode.Value)
            {
                case ManipulationMode.Rotate:
                    // Rotate around Z, X, Y
                    focusedObject.gameObject.transform.Rotate(
                      yDelta * ROTATE_FACTOR, (0 - xDelta) * ROTATE_FACTOR, 0, Space.Self);
                    break;
                case ManipulationMode.Scale:
                    var newScale = (1 + (magnitude * SCALE_FACTOR)) * focusedObject.gameObject.transform.localScale;

                    if ((newScale.magnitude >= MIN_SCALE) && (newScale.magnitude <= MAX_SCALE))
                    {
                        focusedObject.gameObject.transform.localScale = newScale;
                    }
                    break;
                case ManipulationMode.Move:
                    focusedObject.gameObject.transform.Translate(xDelta, yDelta, zDelta, Space.World);
                    break;
                default:
                    break;
            }
            focusedObject.lastDelta = cumulativeDelta;
        }
    }
    static void OnManipulationCanceled(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headRay)
    {
        if (focusedObject != null)
        {
            focusedObject.Done();
        }
    }
    static void OnManipulationCompleted(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headRay)
    {
        if (focusedObject != null)
        {
            focusedObject.Done();
        }
    }
    static void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        var phrase = Enum.Parse(typeof(ManipulationMode), args.text);

        if (Enum.IsDefined(typeof(ManipulationMode), phrase))
        {
            mode = (ManipulationMode)phrase;

            if (focusedObject != null)
            {
                focusedObject.manipulationState = ManipulationState.Available;
            }
        }
    }
    void Done()
    {
        this.lastDelta = null;
        this.manipulationState = ManipulationState.Idle;
    }
    void OnRenderObject()
    {
        if (lineMaterials == null)
        {
            Debug.Log("No line material");
        }
        else if (
          ((this.manipulationState == ManipulationState.Active) ||
           (this.manipulationState == ManipulationState.Available)) &&
          (mode != null) &&
          (lineMaterials != null))
        {
            var materialEntry = lineMaterials.Where(m => m.mode == mode).Single();

            if (materialEntry.material != null)
            {
                materialEntry.material.SetPass(0);

                GL.Begin(GL.LINES);

                // Points from the first point
                GL.Vertex3(_coordinates[0].x, _coordinates[0].y, _coordinates[0].z);
                GL.Vertex3(_coordinates[1].x, _coordinates[1].y, _coordinates[1].z);

                GL.Vertex3(_coordinates[0].x, _coordinates[0].y, _coordinates[0].z);
                GL.Vertex3(_coordinates[3].x, _coordinates[3].y, _coordinates[3].z);

                GL.Vertex3(_coordinates[0].x, _coordinates[0].y, _coordinates[0].z);
                GL.Vertex3(_coordinates[4].x, _coordinates[4].y, _coordinates[4].z);

                // Points from the second point
                GL.Vertex3(_coordinates[1].x, _coordinates[1].y, _coordinates[1].z);
                GL.Vertex3(_coordinates[5].x, _coordinates[5].y, _coordinates[5].z);

                GL.Vertex3(_coordinates[1].x, _coordinates[1].y, _coordinates[1].z);
                GL.Vertex3(_coordinates[2].x, _coordinates[2].y, _coordinates[2].z);

                // Points from the third point
                GL.Vertex3(_coordinates[3].x, _coordinates[3].y, _coordinates[3].z);
                GL.Vertex3(_coordinates[7].x, _coordinates[7].y, _coordinates[7].z);

                GL.Vertex3(_coordinates[3].x, _coordinates[3].y, _coordinates[3].z);
                GL.Vertex3(_coordinates[2].x, _coordinates[2].y, _coordinates[2].z);

                // Points from the fourth point
                GL.Vertex3(_coordinates[4].x, _coordinates[4].y, _coordinates[4].z);
                GL.Vertex3(_coordinates[7].x, _coordinates[7].y, _coordinates[7].z);

                GL.Vertex3(_coordinates[4].x, _coordinates[4].y, _coordinates[4].z);
                GL.Vertex3(_coordinates[5].x, _coordinates[5].y, _coordinates[5].z);

                // Points from the fifth point
                GL.Vertex3(_coordinates[5].x, _coordinates[5].y, _coordinates[5].z);
                GL.Vertex3(_coordinates[6].x, _coordinates[6].y, _coordinates[6].z);

                // Points from the sixth point
                GL.Vertex3(_coordinates[6].x, _coordinates[6].y, _coordinates[6].z);
                GL.Vertex3(_coordinates[2].x, _coordinates[2].y, _coordinates[2].z);

                GL.Vertex3(_coordinates[6].x, _coordinates[6].y, _coordinates[6].z);
                GL.Vertex3(_coordinates[7].x, _coordinates[7].y, _coordinates[7].z);

                GL.End();

                for (int i = 0; i < 8; i++)
                {
                    DrawCube(_cubeCoordinates[i]);
                }
            }
        }
    }

    void DrawCube(Vector3[] points)
    {
        GL.Begin(GL.QUADS);
        for (int i = 0; i < points.Length; i++)
        {
            GL.Vertex3(points[i].x, points[i].y, points[i].z);
        }
        GL.End();
    }
    void DrawCube(Vector3 centre, float size)
    {
        // draw a cube for the corners
        GL.Begin(GL.QUADS);

        GL.Vertex3(centre.x + size, centre.y + size, centre.z - size);
        GL.Vertex3(centre.x - size, centre.y + size, centre.z - size);
        GL.Vertex3(centre.x - size, centre.y + size, centre.z + size);
        GL.Vertex3(centre.x + size, centre.y + size, centre.z + size);

        GL.Vertex3(centre.x + size, centre.y - size, centre.z + size);
        GL.Vertex3(centre.x - size, centre.y - size, centre.z + size);
        GL.Vertex3(centre.x - size, centre.y - size, centre.z - size);
        GL.Vertex3(centre.x + size, centre.y - size, centre.z - size);

        GL.Vertex3(centre.x + size, centre.y + size, centre.z + size);
        GL.Vertex3(centre.x - size, centre.y + size, centre.z + size);
        GL.Vertex3(centre.x - size, centre.y - size, centre.z + size);
        GL.Vertex3(centre.x + size, centre.y - size, centre.z + size);

        GL.Vertex3(centre.x + size, centre.y - size, centre.z - size);
        GL.Vertex3(centre.x - size, centre.y - size, centre.z - size);
        GL.Vertex3(centre.x - size, centre.y + size, centre.z - size);
        GL.Vertex3(centre.x + size, centre.y + size, centre.z - size);

        GL.Vertex3(centre.x - size, centre.y + size, centre.z + size);
        GL.Vertex3(centre.x - size, centre.y + size, centre.z - size);
        GL.Vertex3(centre.x - size, centre.y - size, centre.z - size);
        GL.Vertex3(centre.x - size, centre.y - size, centre.z + size);

        GL.Vertex3(centre.x + size, centre.y + size, centre.z - size);
        GL.Vertex3(centre.x + size, centre.y + size, centre.z + size);
        GL.Vertex3(centre.x + size, centre.y - size, centre.z + size);
        GL.Vertex3(centre.x + size, centre.y - size, centre.z - size);

        GL.End();
    }

    void LateUpdate()
    {
        var collider = gameObject.GetComponentInChildren<BoxCollider>();

        if (collider != null)
        {
            _coordinates = Positions(collider.bounds);

            for (int i = 0; i < 8; i++)
            {
                _cubeCoordinates[i] = CreateCubePositions(_coordinates[i], 0.025f);
            }
        }
    }

    Vector3[] CreateCubePositions(Vector3 rawCoords, float size)
    {
        Vector3[] ret = new Vector3[24];

        // We want the corner cubes to scale with the gameobject but we don't want their
        // individual size to change in the process. So, remove the scale component..
        //
        var scale = transform.localScale;

        float sizex = size;
        float sizey = size;
        float sizez = size;

        ret[0] = new Vector3(rawCoords.x + sizex, rawCoords.y + sizey, rawCoords.z - sizez);
        ret[1] = new Vector3(rawCoords.x - sizex, rawCoords.y + sizey, rawCoords.z - sizez);
        ret[2] = new Vector3(rawCoords.x - sizex, rawCoords.y + sizey, rawCoords.z + sizez);
        ret[3] = new Vector3(rawCoords.x + sizex, rawCoords.y + sizey, rawCoords.z + sizez);

        ret[4] = new Vector3(rawCoords.x + sizex, rawCoords.y - sizey, rawCoords.z + sizez);
        ret[5] = new Vector3(rawCoords.x - sizex, rawCoords.y - sizey, rawCoords.z + sizez);
        ret[6] = new Vector3(rawCoords.x - sizex, rawCoords.y - sizey, rawCoords.z - sizez);
        ret[7] = new Vector3(rawCoords.x + sizex, rawCoords.y - sizey, rawCoords.z - sizez);

        ret[8] = new Vector3(rawCoords.x + sizex, rawCoords.y + sizey, rawCoords.z + sizez);
        ret[9] = new Vector3(rawCoords.x - sizex, rawCoords.y + sizey, rawCoords.z + sizez);
        ret[10] = new Vector3(rawCoords.x - sizex, rawCoords.y - sizey, rawCoords.z + sizez);
        ret[11] = new Vector3(rawCoords.x + sizex, rawCoords.y - sizey, rawCoords.z + sizez);

        ret[12] = new Vector3(rawCoords.x + sizex, rawCoords.y - sizey, rawCoords.z - sizez);
        ret[13] = new Vector3(rawCoords.x - sizex, rawCoords.y - sizey, rawCoords.z - sizez);
        ret[14] = new Vector3(rawCoords.x - sizex, rawCoords.y + sizey, rawCoords.z - sizez);
        ret[15] = new Vector3(rawCoords.x + sizex, rawCoords.y + sizey, rawCoords.z - sizez);

        ret[16] = new Vector3(rawCoords.x - sizex, rawCoords.y + sizey, rawCoords.z + sizez);
        ret[17] = new Vector3(rawCoords.x - sizex, rawCoords.y + sizey, rawCoords.z - sizez);
        ret[18] = new Vector3(rawCoords.x - sizex, rawCoords.y - sizey, rawCoords.z - sizez);
        ret[19] = new Vector3(rawCoords.x - sizex, rawCoords.y - sizey, rawCoords.z + sizez);

        ret[20] = new Vector3(rawCoords.x + sizex, rawCoords.y + sizey, rawCoords.z - sizez);
        ret[21] = new Vector3(rawCoords.x + sizex, rawCoords.y + sizey, rawCoords.z + sizez);
        ret[22] = new Vector3(rawCoords.x + sizex, rawCoords.y - sizey, rawCoords.z + sizez);
        ret[23] = new Vector3(rawCoords.x + sizex, rawCoords.y - sizey, rawCoords.z - sizez);

        return ret;
    }

    Vector3[] Positions(Bounds bounds)
    {
        Vector3[] verts = new Vector3[8];

        verts[0].x = bounds.center.x - bounds.extents.x;
        verts[0].y = bounds.center.y - bounds.extents.y;
        verts[0].z = bounds.center.z + bounds.extents.z;

        verts[1].x = bounds.center.x - bounds.extents.x;
        verts[1].y = bounds.center.y + bounds.extents.y;
        verts[1].z = bounds.center.z + bounds.extents.z;

        verts[2].x = bounds.center.x + bounds.extents.x;
        verts[2].y = bounds.center.y + bounds.extents.y;
        verts[2].z = bounds.center.z + bounds.extents.z;

        verts[3].x = bounds.center.x + bounds.extents.x;
        verts[3].y = bounds.center.y - bounds.extents.y;
        verts[3].z = bounds.center.z + bounds.extents.z;

        verts[4].x = bounds.center.x - bounds.extents.x;
        verts[4].y = bounds.center.y - bounds.extents.y;
        verts[4].z = bounds.center.z - bounds.extents.z;

        verts[5].x = bounds.center.x - bounds.extents.x;
        verts[5].y = bounds.center.y + bounds.extents.y;
        verts[5].z = bounds.center.z - bounds.extents.z;

        verts[6].x = bounds.center.x + bounds.extents.x;
        verts[6].y = bounds.center.y + bounds.extents.y;
        verts[6].z = bounds.center.z - bounds.extents.z;

        verts[7].x = bounds.center.x + bounds.extents.x;
        verts[7].y = bounds.center.y - bounds.extents.y;
        verts[7].z = bounds.center.z - bounds.extents.z;

        return verts;
    }
    Vector3? lastDelta;
    Vector3[] _coordinates;
    Vector3[][] _cubeCoordinates = new Vector3[8][];
    ManipulationState manipulationState;


    // The static pieces...
    static Manipulations focusedObject;
    static KeywordRecognizer keywordRecognizer;
    static GestureRecognizer recognizer;
    static ManipulationMode? mode;

    // These are all really just fudge factors based on a small set of observations.
    const float ROTATE_FACTOR = 500.0f;
    const float SCALE_FACTOR = 10.0f;
    const float MAX_SCALE = 4.0f;
    const float MIN_SCALE = 0.25f;
}