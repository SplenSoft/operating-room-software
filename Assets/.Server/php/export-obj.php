<?php

$password = $_POST["app_password"];
$id = $_POST["id"];
$objData = $_POST["objData"];
$mtlData = $_POST["mtlData"];

if ($password != "qweasdv413240897fvhw") {
    echo "bad password ".$password;
    die();
}

$zip = new ZipArchive;
if ($zip->open('obj/'.$id.'.zip', ZipArchive::CREATE) === TRUE)
{
    // Add a file new.txt file to zip using the text specified
    $zip->addFromString($id.'.obj', $objData);
    $zip->addFromString($id.'.mtl', $mtlData);
    // All files are added, so close the zip file.
    $zip->close();
}

echo "success";

// clean up old files
$files = glob('obj' . '/*');
$threshold = strtotime('-60 minutes');
  
foreach ($files as $file) {
    if (is_file($file)) {
        if ($threshold >= filemtime($file)) {
            unlink($file);
        }
    }
}
?>