function exportPdf(json) {

    var data = JSON.parse(json);
    const pageWidthPx = 1583;
    const pageHeightPx = 1123;
    const doc = new jsPDF({
        unit: 'px',
        orientation: 'landscape',
        format: [pageWidthPx, pageHeightPx]
    });

    var image1Data = doc.getImageProperties(data.image1);
    var image2Data = doc.getImageProperties(data.image2);

    //console.log(image1Data);

    var maxImageHeight = pageHeightPx * 0.5;
    var startingPos = 10;
    printImage(doc, data.image1, startingPos, 10, image1Data.width, image1Data.height, maxImageHeight, pageWidthPx * 0.33, "image1", actualPrintedWidth => startingPos = actualPrintedWidth + 10);
    printImage(doc, data.image2, startingPos, 10, image2Data.width, image2Data.height, maxImageHeight, pageWidthPx * 0.33, "image2");

    for (const element of data.selectableData) {
        console.log(element);
    }
    //var headers = ["Item 1", "Item 2"];
    doc.table(10, maxImageHeight + 10, data.selectableData, null, {
        autoSize: true
    });

    doc.save("a3.pdf");
}

function printImage(jsPdf, imageData, x, y, imageWidth, imageHeight, maxImageHeight, printedWidth, id, callback) {
    var printedHeight = (printedWidth / imageWidth) * imageHeight;
    if (printedHeight > maxImageHeight) {
        var dif = maxImageHeight / printedHeight;
        printedWidth *= dif;
        printedHeight *= dif;
    }

    console.log("Adding image data to pdf for " + id);
    jsPdf.addImage(imageData, 'png', x, y, printedWidth, printedHeight, id, "none", 0);
    if (callback != null) {
        callback(printedWidth);
    }
}

function getBase64Image(imgUrl, callback) {

    var img = new Image();

    // onload fires when the image is fully loadded, and has width and height

    img.onload = function(){

      var canvas = document.createElement("canvas");
      canvas.width = img.width;
      canvas.height = img.height;
      var ctx = canvas.getContext("2d");
      ctx.drawImage(img, 0, 0);
      var dataURL = canvas.toDataURL("image/png"),
          dataURL = dataURL.replace(/^data:image\/(png|jpg);base64,/, "");

      callback(dataURL); // the base64 string

    };

    // set attributes and src 
    img.setAttribute('crossOrigin', 'anonymous'); //
    img.src = imgUrl;

}