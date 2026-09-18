$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$smokeRoot = Join-Path $projectRoot 'Temp/CoreAdapterSmoke'
$unityData = 'C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Data'
New-Item -ItemType Directory -Force -Path "$smokeRoot/Assets/Editor", "$smokeRoot/Assets/Plugins", "$smokeRoot/Packages", "$smokeRoot/ProjectSettings" | Out-Null
Set-Content -LiteralPath "$smokeRoot/Packages/manifest.json" -Value '{"dependencies":{"com.unity.modules.physics":"1.0.0"}}'
Copy-Item -LiteralPath "$projectRoot/ProjectSettings/ProjectVersion.txt" -Destination "$smokeRoot/ProjectSettings/ProjectVersion.txt"
Copy-Item -LiteralPath "$PSScriptRoot/AdapterSmoke.cs.txt" -Destination "$smokeRoot/Assets/Editor/AdapterSmoke.cs"
$arguments = [Collections.Generic.List[string]]::new()
$arguments.Add('/nologo'); $arguments.Add('/target:library'); $arguments.Add('/langversion:9.0'); $arguments.Add('/nostdlib+')
$arguments.Add('/out:"' + "$smokeRoot/Assets/Plugins/REC.AdapterSmoke.dll" + '"')
[xml]$unityProject = Get-Content -LiteralPath "$projectRoot/Assembly-CSharp.csproj"
foreach ($reference in $unityProject.SelectNodes('//Reference/HintPath')) {
    if (Test-Path -LiteralPath $reference.InnerText) { $arguments.Add('/reference:"' + $reference.InnerText + '"') }
}
$files = @('Assets/3.Script/InGame/Map/MapTarget.cs', 'Assets/3.Script/InGame/Phenomenon/IAnomalyTarget.cs', 'Assets/3.Script/InGame/Interaction/Interact.cs', 'Assets/3.Script/InGame/Session/AnomalyTargetAdapter.cs', 'Assets/3.Script/Data/Loading/DataManager.cs')
Get-ChildItem "$projectRoot/Assets/3.Script/InGame/Core", "$projectRoot/Assets/3.Script/Data/Definitions" -Filter '*.cs' | ForEach-Object { $arguments.Add('"' + $_.FullName + '"') }
foreach ($file in $files) { $arguments.Add('"' + "$projectRoot/$file" + '"') }
[IO.File]::WriteAllLines("$smokeRoot/compile.rsp", $arguments)
& "$unityData/NetCoreRuntime/dotnet.exe" "$unityData/DotNetSdkRoslyn/csc.dll" "@$smokeRoot/compile.rsp"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$process = Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe' -ArgumentList @('-batchmode', '-nographics', '-projectPath', ('"' + $smokeRoot + '"'), '-executeMethod', 'AdapterSmoke.Run', '-logFile', ('"' + "$smokeRoot/smoke.log" + '"')) -WindowStyle Hidden -PassThru
$process.WaitForExit()
Get-Content -LiteralPath "$smokeRoot/adapter-result.txt" -ErrorAction SilentlyContinue
exit $process.ExitCode
