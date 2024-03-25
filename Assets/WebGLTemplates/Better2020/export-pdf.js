
const { jsPDF } = window.jspdf;

function getPdfJson(callback) {
    var searchParams = new URLSearchParams(window.location.search);
    var id = searchParams.get('id');
    var url = "https://www.splensoft.com/ors/php/json/" + id + ".json";
    var xhr = new XMLHttpRequest();
    xhr.open('GET', url, true);
    xhr.responseType = 'json';
    xhr.onload = function() {
      var status = xhr.status;
      if (status === 200) {
        callback(null, xhr.response);
      } else {
        callback(status, xhr.response);
      }
    };
    xhr.send();
}

function exportPdf(data) {

    //var data = JSON.parse(json);
    const pageWidthPx = 1191;
    const pageHeightPx = 842;
    const doc = new jsPDF({
        unit: 'px',
        orientation: 'landscape',
        format: [pageWidthPx, pageHeightPx]
    });

    var image1Data = doc.getImageProperties(data.image1);
    var image2Data = doc.getImageProperties(data.image2);

    //console.log(image1Data);

    var maxImageHeight = pageHeightPx * 0.4;
    var startingPos = 10;
    doc.setFillColor(0, 0, 255); // blue fill
    doc.rect(20, 20, pageWidthPx - 350, 50, "F");
    doc.setTextColor(255);
    doc.setFontSize(36);
    doc.text("Medical City Plano OR6 and OR7 Expansion", 30, 50);
    doc.setFontSize(12);
    doc.text("Anesthesia Boom", 40, 65);
    printImage(doc, data.image1, startingPos, 90, image1Data.width, image1Data.height, maxImageHeight, pageWidthPx * 0.33, "image1", actualPrintedWidth => startingPos = actualPrintedWidth + 30);
    printImage(doc, data.image2, startingPos, 90, image2Data.width, image2Data.height, maxImageHeight, pageWidthPx * 0.33, "image2");
    getBase64Image("floor_finish.png", floorFinishImageData => {
        //var floorFinishImageProperties = doc.getImageProperties(floorFinishImageData);
        doc.addImage(floorFinishImageData, 'png', 0, maxImageHeight + 90, pageWidthPx - 300, 20, "floor_finish", "none", 0);

        for (const element of data.selectableData) {
            console.log(element);
        }
        //var headers = ["Item 1", "Item 2"];
        doc.setTextColor(0);
        doc.setFontSize(12);
        // doc.table(10, maxImageHeight + 130, data.selectableData, null, {
        //     autoSize: true
        // });

        const margin = {
            left: 15,
            right: 15,
            top: maxImageHeight + 130,
            bottom: 20,
          };

          // space between each section
        const spacing = 5;
        const sections = 4;

        const printWidht = doc.internal.pageSize.width- (margin.left + margin.right);
        const sectionWidth = (printWidht - ((sections - 1) * spacing)) / sections;
        
        doc.autoTable({
            //head: [['ID', 'Name', 'Email']],
            body: data.selectableData,
            tableWidth: sectionWidth,
            margin,
            //cellWidth: 'auto',
            rowPageBreak: 'avoid', // avoid breaking rows into multiple sections
            didDrawPage({table, pageNumber}) {
              const docPage = doc.internal.getNumberOfPages();
              const nextShouldWrap = pageNumber % sections;
        
              if (nextShouldWrap) {
                
                // move to previous page, so when autoTable calls
                // addPage() it will still be the same current page
                doc.setPage(docPage - 1);
        
                var sectionWidth = 0;
                
                table.columns.forEach((element) => sectionWidth += element.width);

                // change left margin which will controll x position
                table.settings.margin.top = maxImageHeight + 130;
                table.settings.margin.left += sectionWidth + spacing;

              } else {
                
                // reset left margin for the first section in every page
                table.settings.margin.left = margin.left;
              }
            }
          });

        getBase64Image("logo.png", logoImageData => {
            var lowerRightAreaWidth = 200;
            var lowerRightAreaX = pageWidthPx - 520;
            var lowerRightAreaYStart = pageHeightPx - 400;
            var lowerRightAreaCurrentY = lowerRightAreaYStart + 70;
            var textXStart = lowerRightAreaX + 5;
            var textCurrentYOffset = 9;
            var rectHeight = 12;

            doc.addImage(logoImageData, 'png', lowerRightAreaX, lowerRightAreaYStart, lowerRightAreaWidth, 70, "logo", "none", 0);
            doc.rect(lowerRightAreaX, lowerRightAreaCurrentY, lowerRightAreaWidth, rectHeight);
            doc.text("Account Name: Medical City Plano", textXStart, lowerRightAreaCurrentY + textCurrentYOffset);

            lowerRightAreaCurrentY += rectHeight;
            doc.rect(lowerRightAreaX, lowerRightAreaCurrentY, lowerRightAreaWidth, rectHeight);
            doc.text("Account Address: 3901 W. 15th St.", textXStart, lowerRightAreaCurrentY + textCurrentYOffset);

            lowerRightAreaCurrentY += rectHeight;
            doc.rect(lowerRightAreaX, lowerRightAreaCurrentY, lowerRightAreaWidth, rectHeight);
            doc.text("                        Plano, TX 75075", textXStart, lowerRightAreaCurrentY + textCurrentYOffset);

            lowerRightAreaCurrentY += rectHeight;
            doc.rect(lowerRightAreaX, lowerRightAreaCurrentY, lowerRightAreaWidth, rectHeight);
            doc.text("Project Name: OR6 and OR7 Expansion", textXStart, lowerRightAreaCurrentY + textCurrentYOffset);

            lowerRightAreaCurrentY += rectHeight;
            doc.rect(lowerRightAreaX, lowerRightAreaCurrentY, lowerRightAreaWidth, rectHeight);
            doc.text("Project #: IU-10362", textXStart, lowerRightAreaCurrentY + textCurrentYOffset);

            lowerRightAreaCurrentY += rectHeight;
            doc.rect(lowerRightAreaX, lowerRightAreaCurrentY, lowerRightAreaWidth, rectHeight);
            doc.text("Quote Reference #: 1275", textXStart, lowerRightAreaCurrentY + textCurrentYOffset);

            // lowerRightAreaCurrentY += rectHeight;
            // doc.rect(lowerRightAreaX, lowerRightAreaCurrentY, lowerRightAreaWidth, rectHeight);
            // //doc.text("Quote Reference #: 1275", textXStart, lowerRightAreaCurrentY + textCurrentYOffset);

            doc.save("a3.pdf");
        });
        
    });
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

getPdfJson((status, json) => {
    console.log(status);
    console.log(json);
    exportPdf(json);
});