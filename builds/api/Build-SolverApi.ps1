# A build script for building the WatneyAstrometry.WebApi project
# and producing single-file binaries for all operating systems.

$ErrorActionPreference = "Stop";

$project = "$PSScriptRoot/../../src/WatneyAstrometry.WebApi";
$targets = @(
  'osx-x64',
  'linux-arm64',
  'linux-x64',
  'win-x64'
);

foreach($target in $targets) {
  $outputDir = "$PSScriptRoot/binaries/$target";
  $packageDir = "$outputDir/package";
  
  dotnet publish -r $target -c Release -p:PublishTrimmed=true -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true `
	-o $outputDir $project;
	
  Remove-Item -Force -Recurse -Path $packageDir -ErrorAction Ignore;
  New-Item -ItemType Directory -Force -Path $packageDir ;
  
  $exe = "$outputDir/watney-api";
  if($target -eq 'win-x64') {
    $exe = $exe + '.exe'
  }

  $configFile = "$outputDir/config.template.yml";
  $noticeFiles = @(
    "$PSScriptRoot/notices/LICENSE",
	"$PSScriptRoot/notices/NOTICE",
	"$PSScriptRoot/notices/README"
  );
	
  Copy-Item -Path $exe -Destination $packageDir;
  Copy-Item -Path $configFile -Destination "$packageDir/config.yml";
  
  foreach($f in $noticeFiles) {
    Copy-Item -Path $f -Destination $packageDir;
  }
  
}

