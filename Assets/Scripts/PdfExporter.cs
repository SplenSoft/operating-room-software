using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Linq;

public class PdfExporter : MonoBehaviour
{
    public class PdfImageData
    {
        public string Path { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }

    public static async void ExportElevationPdf(
    List<PdfImageData> imageData, 
    List<Selectable> selectables, 
    string title, 
    string subtitle)
    {
        string image1 = "";
        string image2 = "";
        for (int i = 0; i < imageData.Count; i++)
        {
            var item = imageData[i];
            byte[] imageArray = System.IO.File.ReadAllBytes(item.Path);
            string base64ImageRepresentation = Convert.ToBase64String(imageArray);
            //Debug.Log(base64ImageRepresentation);
            if (i == 0)
            {
                image1 = base64ImageRepresentation;
            }
            else
            {
                image2 = base64ImageRepresentation;
            }
        }

        SimpleJSON.JSONObject node = new();
        node.Add("image1", image1);
        node.Add("image2", image2);
        node.Add("title", title);
        node.Add("subtitle", subtitle);
        SimpleJSON.JSONArray selectableArray = new();

        //SimpleJSON.JSONObject selectableData = new();
        //selectableData.Add("Item", "Break System");
        //selectableData.Add("Value", "Electric");
        //selectableArray.Add(selectableData);

        //SimpleJSON.JSONObject selectableData2 = new();
        //selectableData2.Add("Item", "Arm Type");
        //selectableData2.Add("Value", "MediLift Spring Arm");
        //selectableArray.Add(selectableData2);

        selectables
        .OrderBy(x => x.transform.GetParentCount())
        .ToList()
        .ForEach(item =>
        {
            var metaData = item.RelatedSelectables[0].MetaData;

            var matchingItem = ObjectMenu.Instance.ObjectMenuItems
                .FirstOrDefault(x => item.RelatedSelectables[0].GUID == x.SelectableData.AssetBundleName);

            if (matchingItem != null) 
            {
                metaData = matchingItem.SelectableMetaData;
            }

            string itemName = metaData.Name;

            if (!string.IsNullOrWhiteSpace(item.MetaData.SubPartName))
            {
                itemName += " " + item.MetaData.SubPartName;
            }

            if (metaData.Categories.Contains("Service Head Services") || 
            metaData.Name.Contains("Blank Plate") || 
            metaData.Name.Contains("Service Head Rails"))
            {
                return;
            }
            else if (metaData.Categories.Contains("High Voltage Services") ||
            metaData.Categories.Contains("Low Voltage Services"))
            {
                SimpleJSON.JSONObject selectableData = new();
                selectableData.Add("Item", "Service Head Attachment");
                selectableData.Add("Value", itemName);
                selectableArray.Add(selectableData);
            }
            else if (item.RelatedSelectables[0] == item)
            {
                SimpleJSON.JSONObject selectableData = new();
                selectableData.Add("Item", "Part/Attachment");
                selectableData.Add("Value", itemName);
                selectableArray.Add(selectableData);
            } 

            if (item.ScaleLevels.Count > 0) 
            {
                SimpleJSON.JSONObject selectableData = new();
                selectableData.Add("Item", itemName + " length");
                selectableData.Add("Value", item.CurrentScaleLevel.Size * 1000f + "mm");
                selectableArray.Add(selectableData);
            }
        });
        node.Add("selectableData", selectableArray);

        string id = Guid.NewGuid().ToString();

        Dictionary<string, string> formFields = new()
        {
            { "data", node.ToString() },
            { "app_password", "qweasdv413240897fvhw" },
            { "id", id }
        };

        using UnityWebRequest request = UnityWebRequest.Post("http://www.splensoft.com/ors/php/export-pdf-elevation.php", formFields);
        request.SendWebRequest();

        while (!request.isDone)
        {
            await Task.Yield();
            if (!Application.isPlaying) return;
        }

        Debug.Log(request.downloadHandler.text);

        if (request.responseCode != 200)
        {
            Debug.LogError(request.responseCode);
        }

        if (request.error != null)
        {
            Debug.LogError(request.error);
            return;
        }

        if (request.downloadHandler.text == "bad password")
        {
            Debug.LogError("Bad app password");
            return;
        }

        if (request.downloadHandler.text == "success")
        {
            Application.OpenURL("http://www.splensoft.com/ors/pdf.html?id=" + id);
        }
        else
        {
            Debug.LogError("Something went wrong while getting PDF URL");
            Debug.LogError(request);
        }
    }
}