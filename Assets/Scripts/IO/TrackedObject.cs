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
        public string parent;
        public Selectable.ScaleLevel scaleLevel;
    }

    private Data data;

    public Data GetData()
    {
        data.objectName = gameObject.name;
        GetGUIDs(gameObject);

        data.pos = transform.position;
        data.rot = transform.rotation;

        if(gameObject.TryGetComponent<Selectable>(out Selectable s))
        {
            data.scaleLevel = s.CurrentScaleLevel;
        }

        return data;
    }

    public Selectable.ScaleLevel GetScaleLevel()
    {
        if(data.scaleLevel == null) return null;

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

    public void StoreValues(TrackedObject.Data d)
    {
        data.scaleLevel = d.scaleLevel;
        data.pos = d.pos;
        data.rot = d.rot;
    }

    void GetGUIDs(GameObject go)
    {
        if(gameObject.TryGetComponent<Selectable>(out Selectable s))
        {
            data.instance_guid = s.guid.ToString();
            if(s.GUID == "" || s.GUID == null)
            {
                data.parent = ConfigurationManager.GetGameObjectPath(gameObject);
            }
            else
            {
                data.global_guid = s.GUID;
            }

            if(s.ParentAttachmentPoint != null) // This selectable is a child of a configuration, assign AttachmentPoint guid to it's parent ref
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