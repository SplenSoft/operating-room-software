const jsPDF = window.jspdf;

function exportPdf(json)
{
    //var data = JSON.parse(json);

    // Landscape export, 2×4 inches
    const doc = new jsPDF();

    doc.text("Hello world!", 10, 10);
    doc.save("a4.pdf");
}