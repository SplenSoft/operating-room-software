using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp;
#endif

public class PdfExporter : MonoBehaviour
{
    public class PdfImageData
    {
        public string Path { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }

    public static void ExportElevationPdf(List<PdfImageData> imageData)
    {
#if UNITY_EDITOR
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage();

        // 11x17" landscape
        page.Orientation = PageOrientation.Landscape;
        page.Width = 17 * 72;
        page.Height = 11 * 72;

        XGraphics gfx = XGraphics.FromPdfPage(page);

        double printedWidth = page.Width * 0.33;

        for (int i = 0; i < imageData.Count; i++)
        {
            var item = imageData[i];
            XImage image = XImage.FromFile(item.Path);

            double printedHeight = (printedWidth / item.Width) * item.Height;
            gfx.DrawImage(image, (i * printedWidth) + 36, 144, printedWidth, printedHeight);
        }
        //const string fileNamePDF = "ExportedArmAssemblyElevation.pdf";
        string fileNamePDF = EditorUtility.SaveFilePanel("Export .png file", "", "ExportedArmAssemblyElevation", "pdf");
        document.Save(fileNamePDF);

#elif UNITY_WEBGL
        MemoryStream stream = new MemoryStream();
        document.Save(stream, false);
        byte[] bytes = stream.ToArray();
        string bitString = BitConverter.ToString(bytes);
        WebGLExtern.SaveStringToFile(bitString, "pdf");
#else
        throw new Exception("Not supported on this platform");
#endif
    }
}
