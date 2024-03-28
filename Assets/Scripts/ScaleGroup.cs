using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RTG;
using Unity.Collections;
using UnityEngine;

public class ScaleGroup : MonoBehaviour
{
    public static List<ScaleGroup> ScaleGroupEntries = new List<ScaleGroup>();
    [field: SerializeField] public string id { get; private set; }

    private Selectable selectable;

    public bool Initialized { get; private set; }= false;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => selectable.Started);

        ScaleGroup cousin = ScaleGroupEntries.FirstOrDefault(x => x.id == id);

        if (cousin != null && transform.root != transform)
        {
            if (cousin.gameObject.TryGetComponent(out Selectable cousinSelectable))
            {
                if (gameObject.GetComponent<Selectable>().ScaleLevels.Count == 0)
                {
                    Debug.LogWarning($"{cousin.name} => {gameObject.name} = Lossy {cousin.transform.lossyScale.z}");
                    ScaleZ(id, cousin.transform.lossyScale.z);
                }
                else
                {
                    Debug.LogWarning($"{cousin.name} => {gameObject.name} = ScaleLevel {cousinSelectable.CurrentScaleLevel.Size}");
                    ScaleLevel(id, cousinSelectable.CurrentScaleLevel);
                }
            }
            else
            {
                Debug.LogWarning($"{cousin.name} => {gameObject.name} = Lossy {cousin.transform.lossyScale.z}");
                ScaleZ(id, cousin.transform.lossyScale.z);
            }
        }

        ScaleGroupEntries.Add(this);

        Invoke("DelayedListeners", 1f);

        Initialized = true;
    }

    void DelayedListeners()
    {
        if (selectable.ScaleLevels.Count != 0)
        {
            ScaleGroupManager.OnScaleLevelChanged += ScaleLevel;
        }
        else
        {
            ScaleGroupManager.OnZScaleChanged += ScaleZ;
        }
    }

    void OnDestroy()
    {
        if (selectable.ScaleLevels.Count != 0)
        {
            ScaleGroupManager.OnScaleLevelChanged -= ScaleLevel;
        }
        else
        {
            ScaleGroupManager.OnZScaleChanged -= ScaleZ;
        }
    }

    void ScaleLevel(string changedID, Selectable.ScaleLevel scaleLevel)
    {
        Selectable.ScaleLevel myScaleLevel = selectable.ScaleLevels.First(x => x.Size == scaleLevel.Size);

        if (changedID != id || selectable.CurrentScaleLevel == myScaleLevel) return;
        GetComponent<Selectable>().SetScaleLevel(myScaleLevel, true);
    }

    void ScaleZ(string changedID, float z)
    {
        if (changedID != id || transform.localScale.z == z || transform.lossyScale.z == z || float.IsNaN(z) || GetComponent<Selectable>().ScaleLevels.Count != 0) return;

        //transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, z);
        transform.SetWorldScale(new Vector3(transform.lossyScale.x, transform.lossyScale.y, z));
        if (transform.parent != null)
        {
            if (transform.parent.TryGetComponent(out Selectable parentSelectable))
            {
                parentSelectable.StoreChildScales();
            }
        }
    }
}
