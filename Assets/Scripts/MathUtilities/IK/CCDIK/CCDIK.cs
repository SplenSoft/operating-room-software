using System;
using System.Linq;
using UnityEngine;

public class CCDIK : MonoBehaviour
{
  public Transform Tooltip;
  public Transform Target;
  public CCDIKJoint[] joints;

  void Start()
  {
    if (joints.Length == 0)
    {
      joints = transform.root.GetComponentsInChildren<CCDIKJoint>();
      Array.Reverse(joints);
      Target = Instantiate(new GameObject(), gameObject.transform.position, new Quaternion(90,90,0, 0)).transform;
      Target.SetParent(transform.root);
      Target.position = transform.position;
    }
  }

  void Update()
  {
    for (int j = 0; j < joints.Length; j++)
    {
      joints[j].Evaluate(Tooltip, Target, j < 2);
    }
  }
}