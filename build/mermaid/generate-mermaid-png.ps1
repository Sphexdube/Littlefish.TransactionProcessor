$docsDir = "..\..\docs\"
$mermaidFiles = Get-ChildItem -Recurse -Filter *.mermaid -Path $docsDir

foreach ($file in $mermaidFiles) {
    $outputFile = "$($file.DirectoryName)\$($file.BaseName).png"
    mmdc -i $file.FullName -o $outputFile
    Write-Output "Converted $($file.FullName) to $outputFile"
}
