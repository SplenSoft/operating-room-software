using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
using PdfSharpCore.Pdf.Advanced;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore;
using System.Diagnostics;
using System.Linq;
#endif

public class PdfExporter : MonoBehaviour
{
    public class PdfImageData
    {
        public string Path { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }

    public static void ExportElevationPdf(List<PdfImageData> imageData, List<Selectable> selectables)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage();

        // 11x17" landscape
        page.Orientation = PageOrientation.Landscape;
        page.Width = 17 * 72;
        page.Height = 11 * 72;

        XGraphics gfx = XGraphics.FromPdfPage(page);

        double printedWidth = page.Width * 0.33;
        double firstItemHeight = 0f;
        double firstItemWidth = 0f;
        double maxHeight = page.Height / 2f;

        for (int i = 0; i < imageData.Count; i++)
        {
            var item = imageData[i];
            XImage image = XImage.FromFile(item.Path);
            if (i == 0)
            {
                double printedHeight = (printedWidth / item.Width) * item.Height;
                firstItemHeight = printedHeight;
                gfx.DrawImage(image, (i * printedWidth) + 36, 0, printedWidth, printedHeight);
            }
            
            if (i == 1)
            {
                printedWidth = (firstItemHeight / item.Height) * item.Width;
                gfx.DrawImage(image, (page.Width * 0.33) + 72, 0, printedWidth, firstItemHeight);
            }
        }

        var floorImagePath = Application.dataPath + "/StreamingAssets/floor_finish.png";
        XImage image_floor = XImage.FromFile(floorImagePath);
        gfx.DrawImage(image_floor, 36, firstItemHeight, page.Width, 50);

        //table

        XStringFormat format = new XStringFormat();
        format.LineAlignment = XLineAlignment.Near;
        format.Alignment = XStringAlignment.Near;
        var tf = new XTextFormatter(gfx);

        // Row elements
        int el1_width = 200;
        int el2_width = 80;

        // page structure options
        double lineHeight = 20;
        int marginLeft = 36;
        int marginTop = (int)(firstItemHeight + 50 + 10);

        int el_height = 30;
        int rect_height = 17;

        int interLine_X_1 = 2;
        int interLine_X_2 = 2 * interLine_X_1;

        int offSetX_1 = el1_width;
        int offSetX_2 = el1_width + el2_width;

        XSolidBrush rect_style1 = new XSolidBrush(XColors.LightGray);
        XSolidBrush rect_style2 = new XSolidBrush(XColors.DarkGreen);
        XSolidBrush rect_style3 = new XSolidBrush(XColors.Red);

        XFont fontParagraph = new XFont("Verdana", 12, XFontStyle.Regular);

        selectables = selectables.Where(item => item.ScaleLevels.Count > 0).ToList();
        for (int i = 0; i < selectables.Count; i++)
        {
            var seletableData = selectables[i];
            double dist_Y = lineHeight * (i + 1);
            double dist_Y2 = dist_Y - 2;

            // header della G
            if (i == 0)
            {
                gfx.DrawRectangle(rect_style2, marginLeft, marginTop, el1_width + el2_width + 2, rect_height);

                tf.DrawString("Item", fontParagraph, XBrushes.White,
                              new XRect(marginLeft, marginTop, el1_width, el_height), format);

                tf.DrawString("Value", fontParagraph, XBrushes.White,
                              new XRect(marginLeft + offSetX_1 + interLine_X_1, marginTop, el2_width, el_height), format);

                gfx.DrawRectangle(rect_style1, marginLeft, dist_Y2 + marginTop, el1_width, rect_height);
            }

            gfx.DrawRectangle(rect_style1, marginLeft, marginTop + dist_Y2, el1_width, rect_height);
            tf.DrawString(

                seletableData.Name + " length",
                fontParagraph,
                XBrushes.Black,
                new XRect(marginLeft, marginTop + dist_Y, el1_width, el_height),
                format);

            //ELEMENT 2 - BIG 380
            gfx.DrawRectangle(rect_style1, marginLeft + offSetX_1 + interLine_X_1, dist_Y2 + marginTop, el2_width, rect_height);
            tf.DrawString(
                seletableData.CurrentScaleLevel.Size * 1000f + "mm",
                fontParagraph,
                XBrushes.Black,
                new XRect(marginLeft + offSetX_1 + interLine_X_1, marginTop + dist_Y, el2_width, el_height),
                format);
        }


        //const string fileNamePDF = "ExportedArmAssemblyElevation.pdf";
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        //string fileNamePDF = EditorUtility.SaveFilePanel("Export .pdf file", "", "ExportedArmAssemblyElevation", "pdf");
        //document.Save(fileNamePDF);

        string fileNamePDF = Application.persistentDataPath + $"/ExportedArmAssemblyElevation_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.pdf";
        document.Save(fileNamePDF);
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            Verb = "open",
            FileName = Application.persistentDataPath,
        };

        Process.Start(startInfo);
        
#endif
        return;
#endif

//#elif UNITY_WEBGL
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

        //MemoryStream stream = new MemoryStream();
        //document.Save(stream, false);
        //byte[] bytes = stream.ToArray();
        //string bitString = BitConverter.ToString(bytes);
        //WebGLExtern.SaveStringToFile(bitString, "pdf");
        SimpleJSON.JSONObject node = new();
        node.Add("image1", image1);
        node.Add("image2", image2);
        SimpleJSON.JSONArray selectableArray = new();
        selectables.ForEach(item =>
        {
            if (item.ScaleLevels.Count > 0) 
            {
                SimpleJSON.JSONObject selectableData = new();
                selectableData.Add("Item", item.Name + " length");
                selectableData.Add("Value", item.CurrentScaleLevel.Size * 1000f + "mm");
                selectableArray.Add(selectableData);
            }
        });
        node.Add("selectableData", selectableArray);
        //Debug.Log(node);
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLExtern.SaveElevationPDF(node.ToString());
#endif
//#else
        //throw new Exception("Not supported on this platform");
//#endif
    }
}
