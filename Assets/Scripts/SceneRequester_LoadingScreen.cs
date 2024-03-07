using SplenSoft.AssetBundles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper to hook up SplenSoft's asset 
/// bundle manager to ORS's loading system
/// </summary>
[RequireComponent(typeof(SceneRequester))]
public class SceneRequester_LoadingScreen : MonoBehaviour
{
    private SceneRequester _sceneRequester;
    private Loading.LoadingToken _loadingToken;

    private void Awake()
    {
        _sceneRequester = GetComponent<SceneRequester>();

        _sceneRequester.OnProgressUpdated
            .AddListener(OnProgressUpdated);
    }

    private void OnDestroy()
    {
        _sceneRequester.OnProgressUpdated
            .RemoveListener(OnProgressUpdated);

        if (_loadingToken != null)
        {
            _loadingToken.Done();
        }
    }

    private void OnProgressUpdated(float progress)
    {
        if (_loadingToken == null && progress < 1) 
        {
            _loadingToken = Loading.GetLoadingToken();
        }

        _loadingToken.SetProgress(progress);
    }
}