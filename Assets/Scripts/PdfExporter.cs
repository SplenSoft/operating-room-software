using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class PdfExporter : MonoBehaviour
{
    public class PdfImageData
    {
        public string Path { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }

    public static async void ExportElevationPdf(List<PdfImageData> imageData, List<Selectable> selectables)
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
        SimpleJSON.JSONArray selectableArray = new();

        SimpleJSON.JSONObject selectableData = new();
        selectableData.Add("Item", "Break System");
        selectableData.Add("Value", "Electric");
        selectableArray.Add(selectableData);

        SimpleJSON.JSONObject selectableData2 = new();
        selectableData2.Add("Item", "Arm Type");
        selectableData2.Add("Value", "MediLift Spring Arm");
        selectableArray.Add(selectableData2);

        selectables.ForEach(item =>
        {
            if (item.ScaleLevels.Count > 0) 
            {
                SimpleJSON.JSONObject selectableData = new();
                selectableData.Add("Item", item.MetaData.Name + " length");
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