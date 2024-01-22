using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoredValues : MonoBehaviour
{
    [field: SerializeField] public Transform[] trans { get; private set; }
}
