$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$unityData = 'C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Data'
$output = Join-Path $projectRoot 'Temp/CoreChecks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
[xml]$unityProject = Get-Content -LiteralPath (Join-Path $projectRoot 'Assembly-CSharp.csproj')
$arguments = [Collections.Generic.List[string]]::new()
$arguments.Add('/nologo'); $arguments.Add('/target:library'); $arguments.Add('/langversion:9.0'); $arguments.Add('/nostdlib+')
$arguments.Add('/out:"' + (Join-Path $output 'REC.CoreChecks.Unity.dll') + '"')
foreach ($reference in $unityProject.SelectNodes('//Reference/HintPath')) {
    $path = $reference.InnerText
    if (Test-Path -LiteralPath $path) { $arguments.Add('/reference:"' + $path + '"') }
}
foreach ($name in @('Unity.Cinemachine', 'Unity.InputSystem', 'Unity.TextMeshPro', 'Unity.Mathematics', 'UnityEngine.UI')) {
    $arguments.Add('/reference:"' + (Join-Path $projectRoot "Library/ScriptAssemblies/$name.dll") + '"')
}
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/3.Script') -Recurse -Filter '*.cs' | ForEach-Object { $arguments.Add('"' + $_.FullName + '"') }
$responseFile = Join-Path $output 'compile.rsp'
[IO.File]::WriteAllLines($responseFile, $arguments)
& "$unityData/NetCoreRuntime/dotnet.exe" "$unityData/DotNetSdkRoslyn/csc.dll" "@$responseFile"
exit $LASTEXITCODE
