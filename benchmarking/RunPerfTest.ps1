# A script that reads a JSON config, runs the solver accordingly
# and saves the benchmarking output as CSV.
param(
    [Parameter(Mandatory=$true)]
    [string]
    $Config,

    [Parameter(Mandatory=$true)]
    [string]
    $SolverExe
)

$ErrorActionPreference = "Stop"

$defaultArgs = @(    
    "--benchmark",
    "--extended"
);

$cJson = Get-Content -Raw -Encoding ascii $Config;
$c = ConvertFrom-Json -Depth 10 -InputObject $cJson;

$tableFields = @("Image", "Width px", "Height px", "Success", 
    "Stars detected", "Stars used", "Sampling", "Image read (s)", 
    "Star detection (s)", "Quad formation (s)", "Solver (s)", "Full process (s)",
    "Field radius");

$headerRow = [string]::Join(";", $tableFields);
Set-Content -Encoding ascii -Path $c.output $headerRow;

Write-Host "Starting..."

for($s = 0; $s -lt $c.sampling.Length; $s++) {
    $sampling = $c.sampling[$s];
    Write-Host "Sampling $sampling";

    for($i = 0; $i -lt $c.images.Length; $i++) {
        $image = $c.images[$i];
        Write-Host "  Image $image";

        $args = @(
            $c.mode,
            "--sampling", $c.sampling[$s],
            "-i", $image
        );
        if($c.mode -eq "blind") {
            $args += @(
                "--min-radius", "0.5",
                "--max-radius", "8"
            );
        }
        else {
            $args += @(
                "--search-radius", "10",
                "-m"
            );
            $imageNearbyParams = $c.imageParams[$i];
            $args += @("--ra", $imageNearbyParams.ra);
            $args += @("--dec", $imageNearbyParams.dec);
            $args += @("--field-radius-range", $imageNearbyParams.field);
            $args += @("--field-radius-steps", $imageNearbyParams.steps);
        }

        $args += $defaultArgs;

        Write-Host "  Running solver...";
        $row = @($c.images[$i]);
        $solverOutput = & $SolverExe @args;

        $row += [regex]::Match($solverOutput, '"imageWidth": (\d+)').Groups[1].Value
        $row += [regex]::Match($solverOutput, '"imageHeight": (\d+)').Groups[1].Value
        $row += [regex]::Match($solverOutput, '"success": (true|false)').Groups[1].Value
        $row += [regex]::Match($solverOutput, '"starsDetected": (\d+)').Groups[1].Value
        $row += [regex]::Match($solverOutput, '"starsUsed": (\d+)').Groups[1].Value
        $row += $c.sampling[$s];
        $row += [regex]::Match($solverOutput, "IMAGEREAD_DURATION: (\d+\.*\d*)").Groups[1].Value
        $row += [regex]::Match($solverOutput, "STARDETECTION_DURATION: (\d+\.*\d*)").Groups[1].Value
        $row += [regex]::Match($solverOutput, "QUADFORMATION_DURATION: (\d+\.*\d*)").Groups[1].Value
        $row += [regex]::Match($solverOutput, "SOLVE_DURATION: (\d+\.*\d*)").Groups[1].Value
        $row += [regex]::Match($solverOutput, "FULL_DURATION: (\d+\.*\d*)").Groups[1].Value
        $row += [regex]::Match($solverOutput, '"fieldRadius": (\d+\.*\d*)').Groups[1].Value


        $rowString = [string]::Join(";", $row);
        Add-Content -Encoding ascii -Path $c.output $rowString;
    }
}


