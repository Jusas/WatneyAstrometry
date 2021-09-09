$ErrorActionPreference = "Stop";

$project = "$PSScriptRoot/../src/WatneyAstrometry.SolverApp";
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
  
  $exe = "$outputDir/watney-solve";
  if($target -eq 'win-x64') {
    $exe = $exe + '.exe'
  }

  $configFile = "$outputDir/watney-solve-config.template.yml";
  $noticeFiles = @(
    "$PSScriptRoot/notices/LICENSE",
	"$PSScriptRoot/notices/NOTICE",
	"$PSScriptRoot/notices/README"
  );
	
  Copy-Item -Path $exe -Destination $packageDir;
  Copy-Item -Path $configFile -Destination "$packageDir/watney-solve-config.yml";
  
  foreach($f in $noticeFiles) {
    Copy-Item -Path $f -Destination $packageDir;
  }
  
}

