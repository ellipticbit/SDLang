param (
)

[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

$BuildNumber = ([int]$env:BUILD_NUMBER) + 1

if ($env:CI_COMMIT_BRANCH -eq "master") {
    Invoke-WebRequest "https://gitlab.com/api/v4/projects/$($env:CI_PROJECT_ID)/variables/BUILD_NUMBER" -Headers @{"PRIVATE-TOKEN"=$env:CI_API_TOKEN} -Body @{value=$BuildNumber} -ContentType "application/x-www-form-urlencoded" -Method "PUT" -UseBasicParsing
}
