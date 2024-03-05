using SplenSoft.AssetBundles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Tracks and handles all assets that will appear 
/// in the <see cref="ObjectMenu"/>. Automatically 
/// populates before an asset bundle build.
/// </summary>
[ManagedAsset]
[CreateAssetMenu(
    fileName = "SelectableAssetBundles", 
    menuName = "ScriptableObjects/SelectableAssetBundles", 
    order = 1)]
public class SelectableAssetBundles : ScriptableObject
{
    public static bool Initialized { get; private set; }

    private static List<SelectableData> SelectableData { get; } = new();

    [field: SerializeField] 
    private List<string> RelativePaths { get; set; } = new();
    
    [SerializeField]
    private List<SelectableData> _selectableData = new();

    [RuntimeInitializeOnLoadMethod]
    private static void OnAppStart()
    {
        Debug.Log("Getting SelectableAssetBundles datas");
        GetDatas();
    }

    /// <summary>
    /// Populates <see cref="SelectableData"/> from CDN. 
    /// Runs on app start. 
    /// Track <see cref="Initialized"/> to know when it's finished
    /// </summary>
    private static async void GetDatas()
    {
        var loadingToken = Loading.GetLoadingToken(); 

        // get all SelectableAssetBundles asset bundles from CDN
        var task = AssetBundleManager.GetAssetBundleNames(nameof(SelectableAssetBundles));
        await task;
        if (!Application.isPlaying) return;

        List<Task<SelectableAssetBundles>> tasks = new();
        var progresses = new float[task.Result.Length];

        for (int i = 0; i < task.Result.Length; i++)
        {
            string assetBundleName = task.Result[i];

            // track download/load progress for loadingToken
            var progress = new Progress<AssetRetrievalProgress>();
            int index = i;
            progress.ProgressChanged += (_, p) => 
            {
                progresses[index] = p.Progress;
                float prog = progresses.Sum() / task.Result.Length;
                loadingToken.SetProgress(prog);
            };

            var assetRetrievalTask = AssetBundleManager
                .GetAsset<SelectableAssetBundles>
                (assetBundleName, progress);

            tasks.Add(assetRetrievalTask);
        }

        // wait for all download/load tasks to complete
        while (tasks.Any(x => !x.IsCompleted)) await Task.Yield();
        if (!Application.isPlaying) return;

        // cache all Selectable Data
        tasks.ForEach(x => SelectableData
                           .AddRange(x.Result._selectableData));

        Initialized = true;
        loadingToken.Done();
    }


    /// <param name="query">Can be SaveLoadGuid or AssetBundleName</param>
    public bool TryGetSelectableData(string query, out SelectableData data)
    {
        data = SelectableData
            .FirstOrDefault(x =>
                string.Compare(x.SaveLoadGuid, query, true) == 0 ||
                string.Compare(x.AssetBundleName, query, true) == 0);

        return data != default;
    }
}