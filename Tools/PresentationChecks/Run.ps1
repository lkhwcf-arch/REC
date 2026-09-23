$ErrorActionPreference = 'Stop'
# NuGet/네트워크/Unity 실행 없이 설치된 .NET 10 참조 어셈블리와 컴파일러만 사용합니다.
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$dotnetPath = (Get-Command dotnet).Source
$runtimeRoot = Split-Path $dotnetPath
$sdk = Get-ChildItem (Join-Path $runtimeRoot 'sdk') -Directory | Where-Object Name -Like '10.*' | Sort-Object Name -Descending | Select-Object -First 1
$pack = Get-ChildItem (Join-Path $runtimeRoot 'packs/Microsoft.NETCore.App.Ref') -Directory | Where-Object Name -Like '10.*' | Sort-Object Name -Descending | Select-Object -First 1
if (!$sdk -or !$pack) { throw '.NET 10 SDK와 참조 팩이 필요합니다.' }
$output = Join-Path $projectRoot 'Temp/PresentationChecks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$arguments = [Collections.Generic.List[string]]::new()
$arguments.Add('/nologo'); $arguments.Add('/target:exe'); $arguments.Add('/langversion:9.0'); $arguments.Add('/nostdlib+')
$arguments.Add('/out:"' + (Join-Path $output 'PresentationChecks.dll') + '"')
Get-ChildItem (Join-Path $pack.FullName 'ref/net10.0') -Filter '*.dll' | ForEach-Object { $arguments.Add('/reference:"' + $_.FullName + '"') }
$sources = @('Tools/PresentationChecks/Program.cs', 'Tools/CoreChecks/UnityStubs.cs', 'Assets/3.Script/InGame/Core/*.cs',
    'Assets/3.Script/Data/Definitions/*.cs', 'Assets/3.Script/Data/Loading/DataManager.cs',
    'Assets/3.Script/Data/Loading/CSVParser.cs', 'Assets/3.Script/Data/Loading/CsvMapper.cs', 'Assets/3.Script/Data/Validation/GameDataValidator.cs')
foreach ($pattern in $sources) {
    Get-ChildItem (Join-Path $projectRoot $pattern) | ForEach-Object { $arguments.Add('"' + $_.FullName + '"') }
}
$response = Join-Path $output 'compile.rsp'
[IO.File]::WriteAllLines($response, $arguments)
& $dotnetPath (Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll') "@$response"
if ($LASTEXITCODE -ne 0) { throw '도메인 검사 컴파일 실패' }
[IO.File]::WriteAllText((Join-Path $output 'PresentationChecks.runtimeconfig.json'), '{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}')
Push-Location $projectRoot
try {
    & $dotnetPath (Join-Path $output 'PresentationChecks.dll')
    if ($LASTEXITCODE -ne 0) { throw '연출 도메인 검사 실패' }
} finally { Pop-Location }
