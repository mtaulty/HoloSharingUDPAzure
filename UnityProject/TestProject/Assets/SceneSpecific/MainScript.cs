using SharedHolograms;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.XR.WSA.Input;

public class MainScript : MonoBehaviour, ICreateGameObjects
{
    public TextMesh StatusDisplayTextMesh;
    public GameObject PositionalModel;

    public void CreateGameObject(string gameObjectSpecifier, Action<GameObject> callback)
    {
        // Right now, we know how to create one type of thing and we do it in the most
        // obvious way but we could do it any which way we like and even get some other
        // componentry to do it for us.
        if (gameObjectSpecifier == "house")
        {
            var gameObject = GameObject.Instantiate(this.PositionalModel);
            gameObject.SetActive(true);
            callback(gameObject);
        }
        else
        {
            // Sorry, only know about "house" right now.
            callback(null);
        }
    }
    void Start()
    {
        var keywords = new[]
        {
            new { Keyword = "done", Handler = (Action)this.OnDoneKeyword }
        };
        SharedHologramsController.Instance.SceneReady += OnSceneReady;
        SharedHologramsController.Instance.Creator.BusyStatusChanged += OnBusyStatusChanged;
        SharedHologramsController.Instance.Creator.HologramCreatedRemotely += OnRemoteHologramCreated;
        SharedHologramsController.Instance.Creator.GameObjectCreator = this;

        this.keywordRecognizer = new KeywordRecognizer(keywords.Select(k => k.Keyword).ToArray());

        this.keywordRecognizer.OnPhraseRecognized += (e) =>
        {
            var understood = false;

            if ((e.confidence == ConfidenceLevel.High) ||
                (e.confidence == ConfidenceLevel.Medium))
            {
                var handler = keywords.FirstOrDefault(k => k.Keyword == e.text.ToLower());

                if (handler != null)
                {
                    handler.Handler();
                    understood = true;
                }
            }
            if (!understood)
            {
                this.SetStatusDisplayText("I might have missed what you said...");
            }
        };
        this.PositionalModel.SetActive(false);
        this.SetStatusDisplayText("waiting...");
    }
    void OnDoneKeyword()
    {
        if (!this.busy)
        {
            this.keywordRecognizer.Stop();

            this.SetStatusDisplayText("working, please wait...");

            if (this.PositionalModel.activeInHierarchy)
            {
                // Get rid of the placeholder.
                this.PositionalModel.SetActive(false);

                // Create the shared hologram in the same place as the placeholder.
                SharedHologramsController.Instance.Creator.Create(
                    "house",
                    this.PositionalModel.transform.position,
                    this.PositionalModel.transform.forward,
                    Vector3.one,
                    gameObject =>
                    {
                        this.SetStatusDisplayText("object created and shared");
                        this.houseGameObject = gameObject;
                        this.AddManipulations();
                    }
                );
            }
        }
    }
    void OnBusyStatusChanged(object sender, BusyStatusChangedEventArgs e)
    {
        this.busy = e.Busy;

        if (e.Busy)
        {
            this.SetStatusDisplayText("working, please wait...");
        }
    }
    void OnSceneReady(object sender, SceneReadyEventArgs e)
    {
        // Are there other devices around or are we starting alone?
        if (e.Status == SceneReadyStatus.OtherDevicesInScene)
        {
            this.SetStatusDisplayText("detected other devices, requesting sync...");
        }
        else
        {
            this.SetStatusDisplayText("detected no other devices...");
            this.PositionalModel.SetActive(true);
            this.SetStatusDisplayText("walk to position the house then say 'done'");
            this.keywordRecognizer.Start();
        }
    }
    void OnRemoteHologramCreated(object sender, HologramEventArgs e)
    {
        this.PositionalModel.SetActive(false);
        this.SetStatusDisplayText("sync'd...");
        this.keywordRecognizer.Stop();
        this.houseGameObject = GameObject.Find(e.ObjectId.ToString());
        this.AddManipulations();
    }
    void AddManipulations()
    {
        this.SetStatusDisplayText("say 'move', 'rotate' or 'scale'");
        this.houseGameObject.AddComponent<Manipulations>();
    }
    void SetStatusDisplayText(string text)
    {
        if (this.StatusDisplayTextMesh != null)
        {
            this.StatusDisplayTextMesh.text = text;
        }
    }
    KeywordRecognizer keywordRecognizer;
    GameObject houseGameObject;
    bool busy;
}