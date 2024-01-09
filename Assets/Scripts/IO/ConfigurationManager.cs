using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using System;

public class ConfigurationManager : MonoBehaviour
{
    public static ConfigurationManager _instance;
    [Tooltip("Contextual display of GUIDs in hierarchy for easier debugging")]
    public bool isDebug = false;
    private Tracker tracker; // The tracker for individual configurations
    private RoomConfiguration roomConfiguration; // overall room configuration, contains collection of trackers

    void Awake()
    {
        if (_instance != null)
            Destroy(this.gameObject);

        _instance = this;

        CreateTracker();
        NewRoomSave();
    }

    /// <summary>
    /// Creates a new tracker to be used with a fresh configuration load
    /// </summary>
    Tracker CreateTracker()
    {
        tracker = new Tracker
        {
            objects = new List<TrackedObject.Data>()
        };
        return tracker;
    }

    /// <summary>
    /// Create a new room configuration to be used with a fresh room load
    /// </summary>
    RoomConfiguration NewRoomSave()
    {
        roomConfiguration = new RoomConfiguration
        {
            collections = new List<Tracker>()
        };
        return roomConfiguration;
    }

    /// <summary>
    /// Saves a configuration (collection of selectable objects from the transform.root).
    /// </summary>
    /// <param name="title">The title/fileName for this grouping</param>
    public void SaveConfiguration(string title)
    {
        CreateTracker();

        TrackedObject[] foundObjects = Selectable.SelectedSelectable.transform.root.GetComponentsInChildren<TrackedObject>(); // finds all the Selectable & AttachmentPoints for this object

        foreach (TrackedObject obj in foundObjects)
        {
            tracker.objects.Add(obj.GetData()); // Add each tracked object, add to our local tracker instance
        }

        //====== SAVING JSON =======
        string json = JsonConvert.SerializeObject(tracker, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore // allows Newtonsoft to go through the loop to serialize entire Position and Quaternion Rotation
        });
        string folder = Application.persistentDataPath + $"/Saved/Configs/";
        string configName = title.Replace(" ", "_") + ".json"; // remove spaces and replace with underscores

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string path = Path.Combine(folder, configName); // ensure proper pathing

        //Overwrite data
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.WriteAllText(path, json);
        Debug.Log($"Saved Config: {path}");

        ObjectMenu.Instance.AddCustomMenuItem(path); // add this configuration to the ObjectMenu
    }

    public void SaveRoom(string title)
    {
        CreateTracker();
        NewRoomSave();

        roomConfiguration.roomDimension = RoomSize.Instance.currentDimensions; // grabs the current dimensions of the RoomSize to be applied on load

        TrackedObject[] foundObjects = FindObjectsOfType<TrackedObject>();

        foreach (TrackedObject obj in foundObjects) // We need to go through each object
        {
            if (obj.transform == obj.transform.root)
            {
                CreateTracker(); // creating trackers as we go
                TrackedObject[] temps = obj.transform.GetComponentsInChildren<TrackedObject>(); // and finding all embedded/attached selectables along with attachment points
                foreach (TrackedObject to in temps)
                {
                    tracker.objects.Add(to.GetData()); // add them to their respective tracker
                }

                roomConfiguration.collections.Add(tracker); // and add them to the room tracker collection
            }
        }

        // ======SAVING JSON=========
        string json = JsonConvert.SerializeObject(roomConfiguration, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        string folder = Application.persistentDataPath + $"/Saved/";
        string configName = title.Replace(" ", "_") + ".json";

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string path = Path.Combine(folder, configName);

        //Overwrite data
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.WriteAllText(path, json);
        Debug.Log($"Saved Room: {path}");
    }

    private string attachPointGUID = "C9614497-545A-414A-8452-3B7CF50EE43E"; // this is the prefab GUID for ALL attachment points. DO NOT CHANGE.

    public async Task<GameObject> LoadConfig(string file)
    {
        Debug.Log($"Loading config file at {file}");

        if (File.Exists(file))
        {
            CreateTracker(); 
            string json = File.ReadAllText(file);
            tracker = JsonConvert.DeserializeObject<Tracker>(json);

            newPoints = new List<AttachmentPoint>();
            newObjects = new List<TrackedObject>();

            ProcessTrackedObjects(tracker.objects);
            await ResetObjectPositions(newObjects);
            RandomizeInstanceGUIDs();

            return GetRoot();
        }
        else return null;
    }

    public void LoadRoom(string file)
    {
        Debug.Log($"Clearing default room objects");
        TrackedObject[] existingObjects = FindObjectsOfType<TrackedObject>(); // we need to clear the current room (default objects in scene) to load our new one
        foreach (TrackedObject to in existingObjects) 
        {
            if (to.transform == to.transform.root) Destroy(to.gameObject);
        }

        Debug.Log($"Loading Room at {file}");

        if (File.Exists(file))
        {
            CreateTracker();
            string json = File.ReadAllText(file);
            roomConfiguration = JsonConvert.DeserializeObject<RoomConfiguration>(json);
            GenerateRoomConfig();
        }
    }

    List<TrackedObject> newObjects;
    List<AttachmentPoint> newPoints;
    async void GenerateRoomConfig()
    {
        RoomSize.RoomSizeChanged?.Invoke(roomConfiguration.roomDimension); // apply the saved room dimensions from the json to the RoomSize

        foreach (Tracker t in roomConfiguration.collections) // iterate though each tracker in the collection creating new objects. 
        {
            newPoints = new List<AttachmentPoint>();
            newObjects = new List<TrackedObject>();

            ProcessTrackedObjects(t.objects);
            await ResetObjectPositions(newObjects);
            RandomizeInstanceGUIDs();
        }
    }

    void ProcessTrackedObjects(List<TrackedObject.Data> trackedObjects)
    {
        foreach (TrackedObject.Data to in trackedObjects)
        {
            GameObject go = null;
            if (to.global_guid != attachPointGUID && !string.IsNullOrEmpty(to.global_guid)) // if it is not an AttachPoint, we need to place the Selectable
            {
                go = InstantiateObject(to);
                newObjects.Add(go.GetComponent<TrackedObject>());
            }

            if (to.parent != null)
            {
                if (to.global_guid == null || to.global_guid == "") // embedded selectable component
                {
                    go = ProcessEmbeddedSelectable(to);
                    newObjects.Add(go.GetComponent<TrackedObject>());
                }
                else if (to.global_guid == attachPointGUID) // attachment point component
                {
                    ProcessAttachmentPoint(to);
                }
                else // selectable attached to an attachment point
                {
                    ProcessAttachedSelectable(go, to);
                    newObjects.Add(go.GetComponent<TrackedObject>());
                }
            }
        }
    }

    /// <summary>
    /// Instantiates a object, applies position, rotation, guid, and logs the scale for use later.
    /// </summary>
    /// <param name="to">The JSON structure of this object</param>
    /// <returns>The gameobject of our newly instantiated and setup logic applied</returns>
    GameObject InstantiateObject(TrackedObject.Data to)
    {
        GameObject go = Instantiate(ObjectMenu.Instance.GetPrefabByGUID(to.global_guid));
        go.transform.SetPositionAndRotation(to.pos, to.rot);
        go.name = to.instance_guid;
        go.GetComponent<Selectable>().guid = to.instance_guid;
        LogScale(go.GetComponent<Selectable>(), to);
        return go;
    }

    /// <summary>
    /// Finds and applies the tracked AttachmentPoint information to the prefab included version
    /// </summary>
    /// <param name="to">The JSON structure of this object</param>
    void ProcessAttachmentPoint(TrackedObject.Data to)
    {
        GameObject myself = GameObject.Find(to.parent);
        myself.GetComponent<AttachmentPoint>().guid = to.instance_guid;
        newPoints.Add(myself.GetComponent<AttachmentPoint>());
    }

    /// <summary>
    /// Finds and applies the tracked EmbeddedSelectable (non-root selectable of a prefab) to the prefab included version
    /// </summary>
    /// <param name="to">The JSON structure of this object</param>
    /// <returns>The populated selectable's gameobject is returned</returns>
    GameObject ProcessEmbeddedSelectable(TrackedObject.Data to)
    {
        GameObject go = GameObject.Find(to.parent);
        go.GetComponent<Selectable>().guid = to.instance_guid;
        LogScale(go.GetComponent<Selectable>(), to);
        go.transform.rotation = to.rot;
        return go;
    }

    /// <summary>
    /// Finds and applies the tracked AttachedSelectable (root selectable of a prefab attached to another Selectable) and sets it's parent transform
    /// </summary>
    /// <param name="go">Reference to the gameObject being applied data</param>
    /// <param name="to">The JSON structure of this object</param>
    void ProcessAttachedSelectable(GameObject go, TrackedObject.Data to)
    {
        AttachmentPoint ap = newPoints.Single(s => s.guid == to.parent);
        ap.SetAttachedSelectable(go.GetComponent<Selectable>());
        go.transform.SetParent(ap.gameObject.transform);
        go.GetComponent<Selectable>().ParentAttachmentPoint = ap;
        LogScale(go.GetComponent<Selectable>(), to);
    }

    /// <summary>
    /// Resets the objects position and rotation to match with the JSON strucutre, after a frame to allow other logic to process the correct information
    /// </summary>
    /// <param name="newObjects">The tracked list of new objects that have been created during loading</param>
    async Task ResetObjectPositions(List<TrackedObject> newObjects)
    {
        newObjects.Reverse(); // The list needs to be reversed so that the hierarchy is root downwards. 
        foreach (TrackedObject obj in newObjects)
        {
            ResetScaleLevels(obj);
        }

        await Task.Yield(); // allow time for scaling values to be applied in Selectable

        foreach (TrackedObject obj in newObjects)
        {
            ResetLocalPosition(obj);
        }
    }

    /// <summary>
    /// Randomizes the instance GUIDs of the tracked objects within the configuration so that double loading doesn't have conflicts with GameObject.Find
    /// </summary>
    void RandomizeInstanceGUIDs()
    {
        foreach(AttachmentPoint ap in newPoints)
        {
            ap.guid = Guid.NewGuid().ToString();
            ap.gameObject.name = ap.guid;
        }

        foreach(TrackedObject to in newObjects)
        {
            to.gameObject.GetComponent<Selectable>().guid = Guid.NewGuid().ToString();
            to.gameObject.name = to.gameObject.GetComponent<Selectable>().guid;
        }
    }

    /// <summary>
    /// Sets the scale of the object
    /// </summary>
    /// <param name="obj">The JSON structure of the object</param>
    void ResetScaleLevels(TrackedObject obj)
    {
        if (obj.GetScaleLevel() != null)
        {
            obj.GetComponent<Selectable>().ScaleLevels.ForEach((item) => item.Selected = false);
            obj.GetScaleLevel().Selected = true;
        }
    }

    /// <summary>
    /// Sets the Local Position & "OriginalLocalPosition" of the object
    /// </summary>
    /// <param name="obj">The JSON structure of the object</param>
    void ResetLocalPosition(TrackedObject obj)
    {
        if (!string.IsNullOrEmpty(obj.GetComponent<Selectable>().GUID) && obj.transform != obj.transform.root)
        {
            obj.transform.localPosition = Vector3.zero;
            obj.GetComponent<Selectable>().OriginalLocalPosition = obj.transform.localPosition;
        }
    }

    /// <summary>
    /// Logs the JSON structure of the object scale to be applied at a later point in execution by ResetScaleLevels()
    /// </summary>
    /// <param name="s">The object's selectable component</param>
    /// <param name="to">The JSON structure of this object</param>
    void LogScale(Selectable s, TrackedObject.Data to)
    {
        if (to.scaleLevel.Selected && to.scaleLevel != s.CurrentScaleLevel)
        {
            s.GetComponent<TrackedObject>().StoreValues(to);
        }
    }

    /// <summary>
    /// Finds the root transform of a generated configuration
    /// </summary>
    /// <returns>Generated configuration's transform.root</returns>
    GameObject GetRoot()
    {
        return newObjects.Single(x => x.transform == x.transform.root).gameObject;
    }

    /// <summary>
    /// Gets the hierachy PATH for a GameObject
    /// </summary>
    /// <param name="obj">The object whoms path you are needing</param>
    /// <returns>string value containing entire editor & engine pathing</returns>
    public static string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }

}
