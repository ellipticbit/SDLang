param (
)

[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

$Major = ([int]$env:MAJOR_BUILD_NUMBER)
$Minor = ([int]$env:MINOR_BUILD_NUMBER)
$BuildNumber = ([int]$env:BUILD_NUMBER) + 1

$ver = '{0}.{1}.{2}' -f $Major, $Minor, $BuildNumber

[string]$file = Get-Content -Path .\Directory.Build.props -Encoding UTF8

$file = $file.Replace("<Version>0.0.0</Version>", "<Version>$($ver)</Version>");
$file = $file.Replace("<FileVersion>0.0.0.0</FileVersion>", "<FileVersion>$($ver)</FileVersion>");
$file = $file.Replace("<AssemblyVersion>0.0.0.0</AssemblyVersion>", "<AssemblyVersion>$($Major).0.0.0</AssemblyVersion>");

$file | Out-File .\Directory.Build.props -Encoding UTF8

Write-Host "Build Version: $($ver)"
