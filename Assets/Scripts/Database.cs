using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR;

internal static class Database
{
    private const string _cacheFolderName = "DatabaseCache";

    private const string _uri = "https://orswebapi-app-20240309191859.ambitioussky-1264637f.eastus.azurecontainerapps.io";
    //private const string _uri = "https://localhost:7285";

    public static bool Initialized { get; private set; }

    /// <summary>
    /// True if database was not modified 
    /// since the last time everything was cached
    /// </summary>
    public static bool IsUpToDate { get; private set; }

    public static bool AnyCacheErrors { get; private set; }

    public static async Task<MetaDataOperationResult> DoMetaDataOperation(
    MetaDataOperationType type, 
    string assetBundleName = null, 
    SelectableMetaData selectableMetaData = null)
    {
        var loadingToken = Loading.GetLoadingToken();
        try
        {
            string selectableMetaDataString = selectableMetaData != null ?
                JsonConvert.SerializeObject(selectableMetaData) :
                null;

            var op = new MetaDataOperation()
            {
                OperationType = type,
                AssetBundleName = assetBundleName,
                SelectableMetaData = selectableMetaDataString
            };

            var json = JsonConvert.SerializeObject(op);
            //Debug.Log(json);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            string uri = _uri + "/metadata";
            
            using var request = UnityWebRequest.Put(uri, bytes);
            request.method = "POST";
            request.SetRequestHeader("Content-Type", "application/json");
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
                loadingToken.SetProgress(operation.progress);
                if (!Application.isPlaying)
                    throw new Exception("App quit while downloading data");
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"{request.result} - {request.responseCode}");
                if (request.downloadHandler != null)
                {
                    Debug.LogError(request.downloadHandler.text);
                }
            }

            return JsonConvert.DeserializeObject<MetaDataOperationResult>
                (request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            return new MetaDataOperationResult
            {
                OperationType = type,
                ResultType = MetaDataOpertaionResultType.Exception,
                Message = $"{ex.GetType().Name} - {ex.Message}"
            };
        }
        finally 
        {
            loadingToken.Done();
        }
    }

    public static async Task<MetaDataOperationResult> SaveMetaData(
    string assetBundleName, 
    SelectableMetaData selectableMetaData)
    {
        string path = Path.Combine(
            Application.persistentDataPath, 
            _cacheFolderName);

        var type = MetaDataOperationType.Set;

        var task = DoMetaDataOperation(
            type,
            assetBundleName,
            selectableMetaData);

        await task;

        var message = JsonConvert.DeserializeObject
            <StoredMetaData>(task.Result.Message);

        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filePath = Path.Combine(path, assetBundleName);

            string serializedJson = JsonConvert
                .SerializeObject(selectableMetaData);

            File.WriteAllText(filePath, serializedJson);

            PlayerPrefs.SetString($"lastModified_{assetBundleName}", message.LastModified.ToString());

            Debug.Log($"Wrote metadata for asset {assetBundleName} to cache");
        }
        catch (Exception ex) 
        {
            Debug.LogError("Failed to save to cache");
            Debug.LogException(ex);
            AnyCacheErrors = true;
            IsUpToDate = false;
        }
        
        return task.Result;
    }

    public static async Task<MetaDataOperationResult> GetMetaData(
    string assetBundleName, SelectableMetaData seedData)
    {
        var loadingToken = Loading.GetLoadingToken();

        try
        {
            string key = $"lastModified_{assetBundleName}";

            if (IsUpToDate)
            {
                // database was not modified since last time we cached everything

                if (TryGetCache(assetBundleName, out var cachedData))
                {
                    return new MetaDataOperationResult
                    {
                        OperationType = MetaDataOperationType.Get,
                        ResultType = MetaDataOpertaionResultType.Success,
                        Message = JsonConvert.SerializeObject(cachedData)
                    };
                }
            }

            if (PlayerPrefs.HasKey(key) &&
            long.TryParse(PlayerPrefs.GetString(key), out long cachedLastModified))
            {
                var lastModifiedTask = DoMetaDataOperation
                    (MetaDataOperationType.GetLastModified,
                    assetBundleName);

                await lastModifiedTask;

                if (!Application.isPlaying)
                    throw new Exception("App quit while downloading");

                if
                // no internet connection or server failure
                (lastModifiedTask.Result.ResultType != MetaDataOpertaionResultType.Success ||

                // cache is up to date
                (!string.IsNullOrEmpty(lastModifiedTask.Result.Message) &&
                long.TryParse(lastModifiedTask.Result.Message, out long lastModified) &&
                cachedLastModified == lastModified) ||

                // doesn't exist on database (should never happen)
                string.IsNullOrEmpty(lastModifiedTask.Result.Message))
                {
                    if (TryGetCache(assetBundleName, out var cachedData))
                    {
                        return new MetaDataOperationResult
                        {
                            OperationType = MetaDataOperationType.Get,
                            ResultType = MetaDataOpertaionResultType.Success,
                            Message = JsonConvert.SerializeObject(cachedData)
                        };
                    }
                }
            }

            var task = DoMetaDataOperation(
                MetaDataOperationType.Get,
                assetBundleName);

            await task;

            if (string.IsNullOrEmpty(task.Result.Message) &&
            task.Result.ResultType == MetaDataOpertaionResultType.Success)
            {
                // data entry does not exist, use seed data to make it
                task = SaveMetaData(assetBundleName, seedData);
                await task;

                if (task.Result.ResultType == MetaDataOpertaionResultType.Success)
                {
                    return new MetaDataOperationResult
                    {
                        OperationType = MetaDataOperationType.Get,
                        ResultType = MetaDataOpertaionResultType.Success,
                        Message = JsonConvert.SerializeObject(seedData)
                    };
                }
                else return task.Result;
            }

            return task.Result;
        }
        catch (Exception ex) 
        { 
            Debug.LogException(ex);
            return new MetaDataOperationResult
            {
                OperationType = MetaDataOperationType.Get,
                ResultType = MetaDataOpertaionResultType.Exception,
                Message = $"{ex.GetType().Name} - {ex.Message}"
            };
        }
        finally
        {
            loadingToken.Done();
        }
    }

    private static bool TryGetCache(
    string assetBundleName,
    out SelectableMetaData selectableMetaData)
    {
        selectableMetaData = null;

        string filePath = Path.Combine
            (Application.persistentDataPath, 
            _cacheFolderName, 
            assetBundleName);

        if (!File.Exists(filePath)) return false;

        try
        {
            string file = File.ReadAllText(filePath);

            selectableMetaData = 
                (SelectableMetaData)JsonConvert
                .DeserializeObject(
                    file, 
                    typeof(SelectableMetaData));
        }
        catch (Exception ex) 
        { 
            Debug.LogException(ex);
            AnyCacheErrors = true;
        }

        return selectableMetaData != null;
    }

    public static async void SetIsUpToDate()
    {
        var task = GetServerLastModified();
        await task;

        if (!Application.isPlaying)
            throw new Exception("App quit while downloading");

        if (task.Result != -1)
        {
            PlayerPrefs.SetString("DatabaseLastModified", task.Result.ToString());
            IsUpToDate = true;
        }
    }

    public static async Task<long> GetServerLastModified()
    {
        var loadingToken = Loading.GetLoadingToken();
        string uri = _uri + "/lastmodified";
        using var request = UnityWebRequest.Get(uri);
        var operation = request.SendWebRequest();

        while (!request.isDone)
        {
            loadingToken.SetProgress(operation.progress);

            await Task.Yield();
            if (!Application.isPlaying)
                throw new Exception("App quit while downloading");
        }

        loadingToken.Done();

        if (request.result != UnityWebRequest.Result.Success)
        {
            // no internet or server malfunction

            Debug.LogError($"Couldn't retrieve database info (no internet?) | response code {request.responseCode}");

            return -1;
        }

        if (!long.TryParse(request.downloadHandler.text, out long serverLastModified))
        {
            // server sent malformed response??

            Debug.LogError("Couldn't parse last modified date/time from server");
            return -1;
        }

        return serverLastModified;
    }

    [RuntimeInitializeOnLoadMethod]
    private static async void OnAppStart()
    {
        if (!PlayerPrefs.HasKey("DatabaseLastModified"))
        {
            Initialized = true;
            return;
        }

        if (!long.TryParse(PlayerPrefs.GetString("DatabaseLastModified"), out long lastModified))
        {
            // something screwed up the playerprefs

            Debug.LogError($"Cached last modified number was corrupted.");

            Initialized = true;
            return;
        }

        var task = GetServerLastModified();
        await task;

        if (!Application.isPlaying)
            throw new Exception("App quit while downloading");

        Initialized = true;

        if (lastModified == task.Result)
        {
            IsUpToDate = true;
        }
    }

    [Serializable]
    public class MetaDataOperation
    {
        public MetaDataOperationType OperationType { get; set; }
        public string AssetBundleName { get; set; }
        public string SelectableMetaData { get; set; }
    }

    [Serializable]
    public class StoredMetaData
    {
        public int ID { get; set; }
        public long LastModified { get; set; }
        public string AssetBundleName { get; set; }
        public string SelectableMetaData { get; set; }
    }

    public enum MetaDataOpertaionResultType
    {
        None,
        Success,
        Error,
        Exception
    }

    public enum MetaDataOperationType
    {
        Get,
        Set,
        GetLastModified
    }

    [Serializable]
    public class MetaDataOperationResult
    {
        public MetaDataOperationType OperationType { get; set; }
        public MetaDataOpertaionResultType ResultType { get; set; }
        public string Message { get; set; }
    }
}