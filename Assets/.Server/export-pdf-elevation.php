<?php

//require('fpdf.php');
require('html2pdf.php');

// $user_email = $_POST["user_email"];
// $first_name = $_POST["first_name"];
// $last_name = $_POST["last_name"];
// $location = $_POST["location"];
// $floor_plan_name = $_POST["floor_plan_name"];
// $floor_plan_options = $_POST["floor_plan_options"];

// $lineRequesterName = "Name: ".$first_name." ".$last_name;
// $lineRequesterLocation = "Location: ".$location;
// $lineRequesterEmail = "Email: ".$user_email;
// $lineFloorPlanName = "Floor Plan: ".$floor_plan_name;
// $lineRequesterPhoneNumber = "Phone Number: ".$phone_number;

$data = 'data:image/png;base64,AAAFBfj42Pj4';

list($type, $data) = explode(';', $data);
list(, $data)      = explode(',', $data);
$data = base64_decode($data);

file_put_contents('/tmp/image.png', $data);

file_put_contents("img.{$type}", $data);

class PDF extends PDF_HTML
{
    // public $lineRequesterName;
    // public $lineRequesterLocation;
    // public $lineRequesterEmail;
    // public $floor_plan_name;
    // public $interior;
    // public $lineRequesterPhoneNumber;

    public function __construct() 
    { 
        // $this->lineRequesterName = $lineRequesterName;
        // $this->lineRequesterLocation = $lineRequesterLocation;
        // $this->lineRequesterEmail = $lineRequesterEmail;
        // $this->floor_plan_name = $floor_plan_name;
        // $this->lineRequesterPhoneNumber = $lineRequesterPhoneNumber;
        parent::__construct();
    } 

    // Page header
    function Header()
    {
        $this->Cell(75.4);
        //$this->Image('logo-rockford-homes-large.png', null, null, 45);
        $this->Ln(6);
        $this->SetFont('LibreBaskerville-Regular', '', 10);
        $this->SetTextColor(46, 125, 100); // Rockford Middle Green
        // $this->Cell(0, 5, $this->lineRequesterName, 0, 1, "L");
        // $this->Cell(0, 5, $this->lineRequesterLocation, 0, 1, "L");
        // $this->Cell(0, 5, $this->lineRequesterEmail, 0, 1, "L");
        // $this->Cell(0, 5, $this->lineRequesterPhoneNumber, 0, 1, "L");
        $this->SetFillColor(46, 125, 100); // Rockford Middle Green
        $this->SetTextColor(255, 255, 255);
        $this->Ln(2);
        $this->SetFont('LibreBaskerville-Regular', '', 16);
        // $this->Cell(0, 10, 'Your ' . $this->floor_plan_name . " Design Summary" . 
        //     ($this->PageNo() > 1 ? "" : ""), 0, 1, "C", true);

        if ($this->PageNo() > 1)
        {
            // $this->SetTextColor(46, 125, 100);
            // $this->SetFont('LibreBaskerville-Regular', 'B', 14);
            // $this->Ln(2);
            // $this->Cell(0, 8, ($this->interior ? "Interior" : "Exterior") . " Design (Continued)", "B", 1, "L");
            // $this->Ln(2);
        }
    }

    // Page footer
    function Footer()
    {
        $this->SetY(-15);
        $this->SetFont('Arial','I',8);
        $this->Cell(0,10,'Page '.$this->PageNo().'/{nb}',0,0,'C');
    }

    // public function SetInteriorOrExterior(bool $interior): void
    // {
    //     $this->interior = $interior;
    // }
}

$pdf = new PDF();

$pdf->AliasNbPages();
$pdf->AddFont('LibreBaskerville-Regular','','LibreBaskerville-Regular.php');
$pdf->AddFont('LibreBaskerville-Regular','B','LibreBaskerville-Bold.php');
$pdf->AddFont('LibreBaskerville-Regular','I','LibreBaskerville-Italic.php');
$pdf->AddPage("L","A3");
$pdf->SetFillColor(255, 255, 255); 
$pdf->SetTextColor(46, 125, 100); // Rockford Middle Green
$pdf->SetDrawColor(46, 125, 100);
$pdf->

//$floorPlanOptionsArray = explode("\n", $floor_plan_options);

// for($i = 0; $i < count($floorPlanOptionsArray); ++$i) {
//     $isInterior = strpos($floorPlanOptionsArray[$i], 'Interior Design') !== false;
//     $isExterior = strpos($floorPlanOptionsArray[$i], 'Exterior Design') !== false;
//     if ($isInterior || $isExterior) {
//         $pdf->SetFont('LibreBaskerville-Regular', 'B', 14);
//         $pdf->Ln(2);
//         $pdf->Cell(0, 8, $floorPlanOptionsArray[$i], "B", 1, "L");
//         $pdf->Ln(2);
//         $pdf->SetInteriorOrExterior($isInterior);
//     }
//     else {
//         $pdf->SetFont('LibreBaskerville-Regular', '', 10);
//         //$pdf->Cell(0, 6, $floorPlanOptionsArray[$i], 0, 1, "L");
//         // $pdf->SetY($pos_Y);
//         // $pdf->SetX($pos_X);
//         $pdf->writeHTML($floorPlanOptionsArray[$i]);
//         $pdf->Ln(6);
//     }
// }

$pdf->output('elevations.pdf', 'D'); 

?>
