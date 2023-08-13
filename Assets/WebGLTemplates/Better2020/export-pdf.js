function exportPdf(json)
{
    //console.log(json);
    var data = JSON.parse(json);
    //console.log(data);
    // Landscape export, 2×4 inches
    const doc = new jsPDF({
        unit: 'px',
        orientation: 'landscape',
        format: [1583, 1123]
    });

    //doc.text("Hello world!", 10, 10);
    var image1Data = doc.getImageProperties(data.image1);
    var image2Data = doc.getImageProperties(data.image2);
    console.log(image1Data);
    var printedWidth = 1583 * 0.33;
    var printedHeight = (printedWidth / image1Data.width) * image1Data.height;

    console.log("Adding image 1 data to pdf");
    doc.addImage(data.image1, 'png', 10, 10, printedWidth, printedHeight, "image1", "none", 0);
    
    printedHeight = (printedWidth / image2Data.width) * image2Data.height;
    console.log("Adding image 2 data to pdf");
    doc.addImage(data.image2, 'png', printedWidth + 10, 10, printedWidth, printedHeight, "image2", "none", 0);
    doc.save("a3.pdf");
}
