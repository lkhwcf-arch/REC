$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$unityData = 'C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Data'
$output = Join-Path $projectRoot 'Temp/CoreChecks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
[xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot 'Assembly-CSharp-Editor.csproj')
$arguments = [Collections.Generic.List[string]]::new()
$arguments.Add('/nologo'); $arguments.Add('/target:library'); $arguments.Add('/langversion:9.0'); $arguments.Add('/nostdlib+'); $arguments.Add('/define:UNITY_EDITOR')
$arguments.Add('/out:"' + (Join-Path $output 'REC.EditorChecks.dll') + '"')
foreach ($reference in $project.SelectNodes('//Reference/HintPath')) {
    $path = $reference.InnerText
    if ((Test-Path -LiteralPath $path) -and $path -notmatch 'Assembly-CSharp.dll') { $arguments.Add('/reference:"' + $path + '"') }
}
foreach ($name in @('Unity.Cinemachine', 'Unity.InputSystem', 'Unity.TextMeshPro', 'Unity.Mathematics', 'UnityEngine.UI')) {
    $arguments.Add('/reference:"' + (Join-Path $projectRoot "Library/ScriptAssemblies/$name.dll") + '"')
}
$arguments.Add('/reference:"' + (Join-Path $output 'REC.CoreChecks.Unity.dll') + '"')
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/3.Script/Editor') -Filter '*.cs' | ForEach-Object { $arguments.Add('"' + $_.FullName + '"') }
$responseFile = Join-Path $output 'editor.rsp'
[IO.File]::WriteAllLines($responseFile, $arguments)
& "$unityData/NetCoreRuntime/dotnet.exe" "$unityData/DotNetSdkRoslyn/csc.dll" "@$responseFile"
exit $LASTEXITCODE
