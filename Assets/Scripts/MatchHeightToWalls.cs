using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using SplenSoft.UnityUtilities;

/// <summary>
/// Used by Additional Wall (extra wall) selectable 
/// to automatically match the height of the all to the ceiling
/// </summary>
public class MatchHeightToWalls : MonoBehaviour
{
    private Selectable _selectable;

    /// <summary>
    /// Populated on awake. Scale will not be updated until all 
    /// of these components' 
    /// <see cref="MoveToRootOnStart.Moved"/> is true
    /// </summary>
    private MoveToRootOnStart[] _moveToRootOnStarts;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();

        _moveToRootOnStarts = GetComponentsInChildren
            <MoveToRootOnStart>();

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
        while (!_selectable.Started && 
        _moveToRootOnStarts.Any(x => !x.Moved)) 
        { 
            await Task.Yield();
            if (!Application.isPlaying)
                throw new AppQuitInTaskException();
        }

        var wall = RoomBoundary.Instances
            .Where(item => item.RoomBoundaryType == RoomBoundaryType.WallWest)
            .First();

        transform.localScale = new Vector3
            (transform.localScale.x, 
            transform.localScale.y, 
            wall.transform.localScale.y);
    }
}