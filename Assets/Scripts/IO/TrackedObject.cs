using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UIElements;

public class TrackedObject : MonoBehaviour
{
    [Serializable]
    public struct Data
    {
        public string objectName;
        public string instance_guid;
        public string global_guid;
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;
        public string parent;
        public Selectable.ScaleLevel scaleLevel;
        public List<string> materialNames;
    }

    private Data data;

    public Data GetData()
    {
        data.objectName = gameObject.name;
        GetGUIDs(gameObject);

        data.pos = transform.position;
        data.rot = transform.rotation;
        data.scale = transform.localScale;

        if (gameObject.TryGetComponent<Selectable>(out Selectable s))
        {
            if (s.ScaleLevels.Count() == 0) data.scaleLevel = null;
            else
                data.scaleLevel = s.CurrentScaleLevel;
        }

        if(gameObject.TryGetComponent(out MaterialPalette palette))
        {
            data.materialNames = new List<string>();
            Material[] materials = palette.meshRenderer.materials;

            foreach(Material material in materials)
            {
                data.materialNames.Add(material.name);
            }
        }

        return data;
    }

    public Selectable.ScaleLevel GetScaleLevel()
    {
        if (data.scaleLevel == null) return null;

        List<Selectable.ScaleLevel> scales = GetComponent<Selectable>().ScaleLevels;

        return scales.First(x => x.Size == data.scaleLevel.Size);
    }

    public Vector3 GetPosition()
    {
        return data.pos;
    }

    public Quaternion GetRotation()
    {
        return data.rot;
    }

    public Vector3 GetScale()
    {
        return data.scale;
    }

    public List<string> GetMaterials()
    {
        return data.materialNames;
    }

    public void StoreValues(TrackedObject.Data d)
    {
        if (d.scaleLevel != null)
            data.scaleLevel = d.scaleLevel;

        if(d.materialNames != null)
        {
            data.materialNames = d.materialNames;
        }

        data.pos = d.pos;
        data.rot = d.rot;
        data.scale = d.scale;
    }

    void GetGUIDs(GameObject go)
    {
        if (gameObject.TryGetComponent<Selectable>(out Selectable s))
        {
            data.instance_guid = s.guid.ToString();
            if (s.GUID == "" || s.GUID == null)
            {
                data.parent = ConfigurationManager.GetGameObjectPath(gameObject);
            }
            else
            {
                data.global_guid = s.GUID;
            }

            if (s.ParentAttachmentPoint != null) // This selectable is a child of a configuration, assign AttachmentPoint guid to it's parent ref
            {
                data.parent = s.ParentAttachmentPoint.guid.ToString();
            }
        }
        else
        {
            AttachmentPoint ap = gameObject.GetComponent<AttachmentPoint>();
            data.instance_guid = ap.guid.ToString();
            data.global_guid = ap.GUID;

            data.parent = ConfigurationManager.GetGameObjectPath(gameObject);
        }
    }
}