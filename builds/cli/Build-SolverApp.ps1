# A build script for building the WatneyAstrometry.SolverApp project
# and producing single-file binaries for all operating systems.

$ErrorActionPreference = "Stop";

$project = "$PSScriptRoot/../../src/WatneyAstrometry.SolverApp";
$targets = @(
  'osx-x64',
  'linux-arm64',
  'linux-arm',
  'linux-x64',
  'win-x64'
);

foreach($target in $targets) {
  $outputDir = "$PSScriptRoot/binaries/$target";
  $packageDir = "$outputDir/package";
  
  dotnet publish -r $target -c Release --self-contained true `
	-o $outputDir $project;
	
  Remove-Item -Force -Recurse -Path $packageDir -ErrorAction Ignore;
  New-Item -ItemType Directory -Force -Path $packageDir ;
  
  $exe = "$outputDir/watney-solve";
  if($target -eq 'win-x64') {
    $exe = $exe + '.exe'
  }

  $configFile = "$outputDir/watney-solve-config.template.yml";
  $noticeFiles = @(
    "$PSScriptRoot/notices/LICENSE",
	"$PSScriptRoot/notices/NOTICE",
	"$PSScriptRoot/notices/README",
	"$PSScriptRoot/notices/CHANGELOG"
  );
	
  Copy-Item -Path $exe -Destination $packageDir;
  Copy-Item -Path $configFile -Destination "$packageDir/watney-solve-config.yml";
  
  foreach($f in $noticeFiles) {
    Copy-Item -Path $f -Destination $packageDir;
  }
  
}

