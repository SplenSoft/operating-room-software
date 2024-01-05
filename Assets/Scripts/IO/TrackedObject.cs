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
    }

    private Data data;

    public Data GetData()
    {
        data.objectName = gameObject.name;
        GetGUIDs(gameObject);

        data.pos = transform.position;
        data.rot = transform.rotation;

        return data;
    }

    void GetGUIDs(GameObject go)
    {
        if(gameObject.TryGetComponent<Selectable>(out Selectable s))
        {
            data.instance_guid = s.guid.ToString();
            data.global_guid = s.GUID;

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

            if(ap.ParentSelectables[0].GUID == "" || ap.ParentSelectables[0].GUID == null)
            {
                data.parent = ConfigurationManager.GetGameObjectPath(ap.ParentSelectables[0].gameObject);
            }
            else
            {
                data.parent = ap.ParentSelectables[0].guid.ToString();
            }
        }
    }
}