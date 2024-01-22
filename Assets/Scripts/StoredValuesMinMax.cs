using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoredValuesMinMax : MonoBehaviour
{
    private Selectable selectable;
    private StoredValues storedValues;

    void Start()
    {
        storedValues = transform.parent.parent.GetComponent<StoredValues>();
        selectable = transform.GetComponent<Selectable>();
        SetMinMax();
    }

    public void SetMinMax()
    {
        if (selectable.TryGetGizmoSetting(GizmoType.Move, Axis.Z, out GizmoSetting setting))
        {
            setting.SetMinMaxValues(
                storedValues.trans[0].localPosition.z,
                storedValues.trans[1].localPosition.z
            );
        }
    }
}
