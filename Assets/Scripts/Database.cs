using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using UnityEngine;

internal static class Database
{
    private const string _cacheFolderName = "DatabaseCache";

    [RuntimeInitializeOnLoadMethod]
    private static async void Test()
    {
        // Initialize the Unity Services Core SDK
        await UnityServices.InitializeAsync();

        // Authenticate by logging into an anonymous account
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        try
        {
            var op = new MetaDataOperation();
            var json = JsonConvert.SerializeObject(op);

            // Call the function within the module and provide the parameters we defined in there
            string result = await CloudCodeService.Instance.CallModuleEndpointAsync("orsunitycloudcode", "DoMetaDataOperation", new Dictionary<string, object> { { "json", json } });

            Debug.Log(result);
        }
        catch (CloudCodeException exception)
        {
            Debug.LogException(exception);
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
        }

        return selectableMetaData != null;
    }

    [Serializable]
    public class MetaDataOperation
    {
        public MetaDataOperationType OperationType { get; set; }
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
}