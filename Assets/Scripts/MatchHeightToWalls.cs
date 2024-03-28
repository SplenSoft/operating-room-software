using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RTG;

public class MatchHeightToWalls : MonoBehaviour
{
    [field: SerializeField] private GameObject[] SkipScaleObjects { get; set; }
    private List<Vector3> SkipOriginalPositions = new List<Vector3>();
    private List<Quaternion> SkipQuaternions = new List<Quaternion>();
    private List<Vector3> SkipOriginalScale = new List<Vector3>();
    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();

        foreach (GameObject go in SkipScaleObjects)
        {
            SkipQuaternions.Add(
                new Quaternion(
                    go.transform.localRotation.x,
                    go.transform.localRotation.y,
                    go.transform.localRotation.z,
                    go.transform.localRotation.w
                )
            );

            SkipOriginalScale.Add(
                new Vector3(
                    go.transform.lossyScale.x,
                    go.transform.lossyScale.y,
                    go.transform.lossyScale.z
                )
            );

            //go.transform.SetParent(null);
        }

        RoomSize.RoomSizeChanged.AddListener(UpdateScale);
    }

    private void OnDestroy()
    {
        RoomSize.RoomSizeChanged.RemoveListener(UpdateScale);
    }

    private void Start()
    {
        UpdateScale(new RoomDimension());
    }

    private async void UpdateScale(RoomDimension dimension)
    {
        while (!selectable.Started) { await Task.Yield(); if (!Application.isPlaying) return; }

        var wall = RoomBoundary.Instances.Where(item => item.RoomBoundaryType == RoomBoundaryType.WallWest).First();

        foreach(GameObject go in SkipScaleObjects)
        {
            SkipOriginalPositions.Add(
                go.transform.position - transform.position
            );

            go.transform.SetParent(null);
        }

        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, wall.transform.localScale.y);

        for (int i = 0; i < SkipScaleObjects.Length; i++)
        {
            Selectable s = SkipScaleObjects[i].GetComponent<Selectable>();
            ScaleGroup sg = SkipScaleObjects[i].GetComponent<ScaleGroup>();

            while (!s.Started && !sg.Initialized) { await Task.Yield(); if (!Application.isPlaying) return; }

            s.SetScaleLevel(s.CurrentScaleLevel, false, true);

            SkipScaleObjects[i].transform.SetParent(transform);

            SkipScaleObjects[i].transform.SetLocalPositionAndRotation(SkipOriginalPositions[i], SkipQuaternions[i]);
            SkipScaleObjects[i].transform.position = transform.position + SkipOriginalPositions[i];
        }
    }
}
