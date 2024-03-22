[CmdletBinding()]
param (
    [Parameter(Mandatory = $true,
        Position = 0,
        ParameterSetName = 'ProjectPath',
        ValueFromPipeline = $true,
        ValueFromPipelineByPropertyName = $true,
        HelpMessage = 'Path to root of the Unity project.')]
    [ValidateNotNullOrEmpty()]
    [string]
    $ProjectPath,

    [Parameter(Mandatory = $true,
        HelpMessage = 'List of package.json files that should also be updated with the new version numbers.')]
    [string[]]
    $PackageJsonFilesToUpdate
)

$ErrorActionPreference = 'Stop'

$ProjectPath = $ProjectPath.Trim()
$PackageJsonFilesToUpdate = $PackageJsonFilesToUpdate | ForEach-Object { $_.Trim() }
$oldLocation = Get-Location
try {
    Set-Location $ProjectPath

    if (-not (Test-Path -Path $PackageJsonFilesToUpdate -PathType Leaf)) {
        throw "One of the provided package.json files doesn't exists: ['$($PackageJsonFilesToUpdate -join "'; '")']"
    }

    $manifestObject = Get-Content -Path 'Packages\manifest.json' -Encoding utf8 | ConvertFrom-Json -Depth 10
    $packagesLockObject = Get-Content -Path "Packages\packages-lock.json" -Encoding utf8 | ConvertFrom-Json -Depth 10
    $availableUpdates = New-Object Collections.Generic.List[PsObject]
    foreach ($dependency in $manifestObject.dependencies.PsObject.Properties) {
        $packageName = $dependency.Name
        $currentVersion = $dependency.Value
        $packageLockData = $packagesLockObject.dependencies.PsObject.Properties[$packageName].Value
        if ($packageLockData.source -ne 'registry') {
            continue
        }

        $repositoryBaseUrl = $packageLockData.url
        $packageMetadata = Invoke-WebRequest -Uri "$repositoryBaseUrl/$packageName" | ConvertFrom-Json -Depth 10
        $latestVersion = $packageMetadata.'dist-tags'.latest
        if ($currentVersion.Contains('-')) {
            # current is pre-release -> allow pre-release
            $latestVersion = $packageMetadata.'dist-tags'.latest
        }
        else {
            $latestVersion = ($packageMetadata.versions.PsObject.Properties | Where-Object { -not $_.Name.Contains('-') } |
                ForEach-Object { [System.Version]::Parse($_.Name) } | Sort-Object -Descending | Select-Object -First 1).ToString()
        }

        if ([System.String]::IsNullOrEmpty($latestVersion)) {
            throw "Failed to get latest version number for package: '$packageName'. Received the following from the registry: '$repositoryBaseUrl': $packageMetadata"
        }

        if ($currentVersion -eq $latestVersion) {
            continue
        }

        Write-Host "Update available for package '$packageName': $currentVersion -> $latestVersion"
        $availableUpdates.Add([PSCustomObject]@{
                Name       = $packageName
                NewVersion = $latestVersion
            })
        $dependency.Value = $latestVersion
    }

    if ($availableUpdates.Count -eq 0) {
        return
    }

    Write-Host "Update Packages/manifest.json of Unity project at: '$ProjectPath'"
    ConvertTo-Json -InputObject $manifestObject -Depth 10  | Set-Content -Path 'Packages\manifest.json' -Encoding utf8

    foreach ($packageJsonFile in $PackageJsonFilesToUpdate) {
        $packageJsonContent = Get-Content -Path $packageJsonFile -Encoding utf8
        $packageObject = $packageJsonContent | ConvertFrom-Json
        $changed = $false
        foreach ($update in $availableUpdates) {
            $dependency = $packageObject.dependencies.PsObject.Properties[$update.Name]
            if ($dependency) {
                $dependency.Value = $update.NewVersion
                $changed = $true
            }
        }

        if (-not $changed) {
            continue
        }

        Write-Host "Update $packageJsonFile of Unity project at: '$ProjectPath'"
        $inputIndent = try { $packageJsonContent[1].Substring(0, $packageJsonContent[1].Length - $packageJsonContent[1].TrimStart().Length) } catch { '  ' }

        # ConvertTo-Json produced a single string with 'double space' as indent, we split the lines and replace the indent with the input indent
        $outputJsonContent = (ConvertTo-Json -InputObject $packageObject).Split([System.Environment]::NewLine)
        $outputJsonContent = $outputJsonContent -replace '(^|\G)  ', $inputIndent
        $outputJsonContent | Set-Content -Path $packageJsonFile -Encoding utf8
    }
}
finally {
    Set-Location $oldLocation
}
