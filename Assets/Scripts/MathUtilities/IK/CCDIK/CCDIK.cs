using System;
using System.Collections.Generic;
using UnityEngine;

public class CCDIK : MonoBehaviour
{
  public Transform Target;
  public List<CCDIKJoint> joints;

  void Start()
  {
    joints = new List<CCDIKJoint>();

    Target = Instantiate(new GameObject(), gameObject.transform.position, new Quaternion(90, 90, 0, 0)).transform;
    Target.SetParent(transform.root);
    Target.position = transform.position;

    FindJoints();
  }

  void OnEnable()
  {
    RecenterTarget();
  }

  public void RecenterTarget()
  {
    if (Target != null)
    {
      Target.position = transform.position;
    }
  }

  void FindJoints()
  {
    Transform parent = this.transform;

    while (parent != null)
    {
      if (parent.TryGetComponent(out Selectable selectable))
      {
        if (parent.parent == null)
        {
          break;
        }

        CCDIKJoint joint = null;
        if (selectable.IsGizmoSettingAllowed(GizmoType.Rotate, Axis.Z))
        {
          joint = selectable.gameObject.AddComponent<CCDIKJoint>();
          joint.axis = new Vector3(0, 0, 1);

          float min = selectable.GetGizmoSettingMinValue(GizmoType.Rotate, Axis.Z);
          float max = selectable.GetGizmoSettingMaxValue(GizmoType.Rotate, Axis.Z);

          joint.maxAngle = Math.Abs(min) + Math.Abs(max);

          if (joint.maxAngle > 180)
            joint.maxAngle = 180;
        }

        if (joint != null)
          joints.Add(joint);
      }

      parent = parent.parent;
    }

    this.enabled = false;
  }

  void Update()
  {
    if (joints.Count > 0)
      for (int j = 0; j < joints.Count; j++)
      {
        joints[j].Evaluate(transform, Target, j == 0);
      }
  }
}