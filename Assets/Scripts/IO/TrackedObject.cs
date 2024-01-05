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
        public Data[] attachments;
    }

    private Data data;

    public Data GetData()
    {
        data.objectName = gameObject.name;
        data.pos = transform.position;
        data.rot = transform.rotation;
        data.instance_guid = gameObject.GetComponent<Selectable>().guid.ToString();
        data.global_guid = gameObject.GetComponent<Selectable>().GUID;

        List<Data> tempData = new List<Data>();
        SearchForAttachments(tempData, transform);
        data.attachments = tempData.ToArray();

        return data;
    }

    void SearchForAttachments(List<Data> d, Transform parent)
    {
        // foreach (Transform go in parent)
        // {
        //     if (go.TryGetComponent<AttachmentPoint>(out AttachmentPoint ap))
        //     {
        //         if (ap.AttachedSelectable[0].gameObject.TryGetComponent(out TrackedObject tracked))
        //         {
        //             d.Add(tracked.GetData());
        //         }
        //     }

        //     if(go.TryGetComponent<Selectable>(out Selectable s))
        //     {
        //         continue;
        //     }

        //     if(go.childCount > 0)
        //     SearchForAttachments(d, go);
        // }
    }
}