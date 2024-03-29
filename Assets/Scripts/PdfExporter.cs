using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using SimpleJSON;
using static UnityEngine.Rendering.DebugUI;

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
    string subtitle,
    List<AssemblyData> assemblyDatas)
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

        JSONObject node = new();
        node.Add("image1", image1);
        node.Add("image2", image2);
        node.Add("title", title);
        node.Add("subtitle", subtitle);

        node.Add("AccountName", UI_ClientMetaData.AccountName);
        node.Add("AccountAddressLine1", UI_ClientMetaData.AccountAddressLine1);
        node.Add("AccountAddressLine2", UI_ClientMetaData.AccountAddressLine2);
        node.Add("ProjectName", UI_ClientMetaData.ProjectName);
        node.Add("ProjectNumber", UI_ClientMetaData.ProjectNumber);
        node.Add("OrderReferenceNumber", UI_ClientMetaData.OrderReferenceNumber);

        List<AssemblyJson> allTables = new List<AssemblyJson>();
        var assemblies = new JSONArray();
        int assId = 1;
        foreach (var assemblyData in assemblyDatas)
        {
            AssemblyJson assembly = new()
            {
                AssemblyId = assId++
            };
            List<string> serviceHeadItems = new();
            List<string> usedServiceHeadItems = new();

            var ordSelectables = assemblyData.OrderedSelectables;

            ordSelectables.ForEach(item =>
            {
                var metaData = item.GetMetadata();

                string itemName = metaData.Name;

                if (metaData.Categories.Contains("High Voltage Services") ||
                metaData.Categories.Contains("Low Voltage Services"))
                {
                    var existing = serviceHeadItems.FirstOrDefault(x => x.StartsWith(itemName));

                    if (existing != default)
                    {
                        var count = 1;
                        var match = Regex.Match(existing, @"\((\d+)\)");
                        if (match.Success)
                        {
                            count = int.Parse(match.Groups[1].Value);
                        }

                        serviceHeadItems.Remove(existing);
                        serviceHeadItems.Add(itemName + $" ({count + 1})");
                    }
                    else
                    {
                        serviceHeadItems.Add(itemName);
                    }
                }
            });

            ordSelectables.ForEach(item =>
            {
                var metaData = item.GetMetadata();

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
                    bool exists = usedServiceHeadItems.Any(x => x.StartsWith(itemName));

                    if (!exists)
                    {
                        JSONObject selectableData = new();
                        selectableData.Add("Item", "Service Head Attachment");
                        selectableData.Add("Value", serviceHeadItems.First(x => x.StartsWith(itemName)));
                        assembly.ItemArray.Add(selectableData);
                        usedServiceHeadItems.Add(itemName);
                    }
                }
                else if (item.RelatedSelectables[0] == item)
                {
                    metaData.PdfData.ForEach(pdfData =>
                    {
                        string value = pdfData.Value.Trim();
                        string table = pdfData.Table.Trim();

                        if (string.Compare(value, "{NAME}", true) == 0)
                        {
                            value = itemName;
                        }

                        if (string.Compare(table, "{ASSEMBLY}", true) == 0)
                        {
                            JSONObject selectableData = new();
                            selectableData.Add("Item", pdfData.Key);
                            selectableData.Add("Value", value);
                            assembly.ItemArray.Add(selectableData);
                        }
                        else
                        {
                            var existing = allTables.FirstOrDefault(x => x.TableName == table);
                            if (existing == default)
                            {
                                existing = new AssemblyJson();
                                allTables.Add(existing);
                            }

                            JSONObject selectableData = new();
                            selectableData.Add("Item", pdfData.Key);
                            selectableData.Add("Value", value);
                            existing.ItemArray.Add(selectableData);
                        }
                    });
                }
                if (item.ScaleLevels.Count > 0)
                {
                    JSONObject selectableData = new();
                    selectableData.Add("Item", itemName + " length");
                    selectableData.Add("Value", item.CurrentScaleLevel.Size * 1000f + "mm");
                    assembly.ItemArray.Add(selectableData);
                }
            });

            assembly.TableName = assemblyData.Title;
            allTables.Add(assembly);
        }

        UI_PdfExportOptions.GetAdditionalData().ForEach(x =>
        {
            AssemblyJson existing;
            var match = Regex.Match(x.Table, @"{(\d)}");
            
            if (match.Success)
            {
                existing = allTables.FirstOrDefault(y => int.Parse(match.Groups[1].Value) == y.AssemblyId);
            }
            else
            {
                existing = allTables.FirstOrDefault(y => x.Table == y.TableName);
            }
            
            if (existing == default)
            {
                existing = new AssemblyJson() 
                { 
                    TableName = x.Table 
                };

                allTables.Add(existing);
            }

            x.Data.ForEach((k, v) =>
            {
                JSONObject selectableData = new();
                selectableData.Add("Item", k);
                selectableData.Add("Value", v);
                existing.ItemArray.Add(selectableData);
            });
        });

        allTables.ForEach(item =>
        {
            item.Bake();
            assemblies.Add(item.JsonObject);
        });

        node.Add("assemblies", assemblies);

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

public class AssemblyJson
{
    /// <summary>
    /// Unused by extra tables
    /// </summary>
    public int AssemblyId { get; set; }
    public string TableName { get; set; }
    public JSONArray ItemArray { get; set; } = new();
    public JSONObject JsonObject { get; set; } = new();

    public void Bake()
    {
        JsonObject.Add("title", TableName);
        JsonObject.Add("selectableData", ItemArray);
    }
 }