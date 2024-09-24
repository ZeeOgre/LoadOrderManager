param (
    [string]$SettingsFile = "..\Properties\Settings.settings",
    [string]$CsprojFilePath = "..\ZO.LoadOrderManager.csproj",
    [string]$AppConfigFilePath = "..\App.config",
    [string]$VersionTxtFilePath = "..\Properties\version.txt",
    [string]$AipFilePath = "..\installer\DMMInstaller.aip",
    [string]$XmlOutputPath = "..\Properties\AutoUpdater.xml",
    [string]$Configuration,
    [string]$AssemblyInfoFilePath = "..\Properties\AssemblyInfo.cs"
)

function Increment-Version {
    param (
        [string]$version
    )
    
    $versionSegments = $version.Split('.')
    if ($versionSegments.Length -ne 4) {
        throw "Version must have four segments (e.g., 1.0.0.0)"
    }
    
    $versionSegments[2] = [long]$versionSegments[2] + 1
    return $versionSegments -join '.'
}

function Update-CsprojVersion {
    param (
        [string]$newVersion,
        [string]$csprojFilePath,
        [switch]$WhatIf
    )
    if ([string]::IsNullOrEmpty($csprojFilePath)) {
        throw "Csproj file path is empty or null."
    }
    
    [xml]$csprojXml = [xml](Get-Content -Path $csprojFilePath -Raw -Encoding UTF8)
    
    $propertyGroupNode = $csprojXml.SelectSingleNode("//PropertyGroup")
    if ($propertyGroupNode -eq $null) {
        throw "Error: PropertyGroup node not found in csproj file."
    }

    $versionNode = $propertyGroupNode.SelectSingleNode("Version")
    if ($versionNode -ne $null) {
        $versionNode.InnerText = $newVersion
    } else {
        $newVersionNode = $csprojXml.CreateElement("Version")
        $newVersionNode.InnerText = $newVersion
        $propertyGroupNode.AppendChild($newVersionNode)
    }

    if ($WhatIf) {
        Write-Output "WhatIf: $csprojFilePath would be updated with new version $newVersion"
    } else {
        $csprojXml.Save($csprojFilePath)
    }
}

function Get-CurrentVersion {
    param (
        [string]$filePath
    )

    [xml]$xml = Get-Content -Path $filePath -Raw -Encoding UTF8

    $namespaceManager = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $namespaceManager.AddNamespace("ns", "http://schemas.microsoft.com/VisualStudio/2004/01/settings")

    $currentVersionNode = $xml.SelectSingleNode("//ns:Setting[@Name='version']/ns:Value", $namespaceManager)
    if ($null -eq $currentVersionNode) {
        throw "Version node not found in settings file."
    }
    $currentVersion = $currentVersionNode.InnerText

    $result = [PSCustomObject]@{
        Version = $currentVersion
        Node = $currentVersionNode
        Xml = $xml
    }

    return $result
}

function Update-SettingsFile {
    param (
        [string]$newVersion,
        [string]$settingsFilePath
    )
    if ([string]::IsNullOrEmpty($settingsFilePath)) {
        throw "Settings file path is empty or null."
    }

    $currentVersionData = Get-CurrentVersion -filePath $settingsFilePath
    $currentVersionData.Node.InnerText = $newVersion
    $currentVersionData.Xml.Save($settingsFilePath)
}

function Update-VersionTxtFile {
    param (
        [string]$newVersion,
        [string]$versionTxtFilePath
    )
    if ([string]::IsNullOrEmpty($versionTxtFilePath)) {
        throw "Version.txt file path is empty or null."
    }

    Set-Content -Path $versionTxtFilePath -Value $newVersion -Encoding UTF8
}

function Update-AppConfig {
    param (
        [string]$newVersion,
        [string]$appConfigFilePath
    )
    if ([string]::IsNullOrEmpty($appConfigFilePath)) {
        throw "App.config file path is empty or null."
    }

    [xml]$appConfigXml = [xml](Get-Content -Path $appConfigFilePath -Raw -Encoding UTF8)
    
    $versionNode = $appConfigXml.SelectSingleNode("//applicationSettings/ZO.LoadOrderManager.Properties.Settings/setting[@name='version']/value")
    if ($versionNode -ne $null) {
        $versionNode.InnerText = $newVersion
        Write-Output "Updated Version in App.config: $newVersion"
    } else {
        throw "Error: Version node not found in App.config."
    }

    $appConfigXml.Save($appConfigFilePath)
}

function Update-AutoUpdaterXml {
    param (
        [string]$newVersion,
        [string]$xmlOutputPath
    )
    if ([string]::IsNullOrEmpty($xmlOutputPath)) {
        throw "AutoUpdater.xml file path is empty or null."
    }

    [xml]$xmlOutput = [xml](Get-Content -Path $xmlOutputPath -Raw -Encoding UTF8)
    
    $versionNode = $xmlOutput.SelectSingleNode("//item/version")
    if ($versionNode -ne $null) {
        $versionNode.InnerText = $newVersion
        Write-Output "Updated Version in AutoUpdater.xml: $newVersion"
    } else {
        throw "Error: Version node not found in AutoUpdater.xml."
    }

    $xmlOutput.Save($xmlOutputPath)
}

function Update-AipFile {
    param (
        [string]$newVersion,
        [string]$aipFilePath
    )
    if ([string]::IsNullOrEmpty($aipFilePath)) {
        throw "Installer.aip file path is empty or null."
    }

    # Load the AIP file as XML
    [xml]$aipXml = [xml](Get-Content -Path $aipFilePath -Raw -Encoding UTF8)
    
    # Update ProductVersion
    $versionNode = $aipXml.SelectSingleNode("//ROW[@Property='ProductVersion']")
    if ($versionNode -ne $null) {
        $versionNode.SetAttribute("Value", $newVersion)
    } else {
        throw "Error: Version node not found in Installer.aip."
    }

    # Update ProductCode with a new GUID
    $productCodeNode = $aipXml.SelectSingleNode("//ROW[@Property='ProductCode']")
    if ($productCodeNode -ne $null) {
        $newGuid = [guid]::NewGuid().ToString()
        $productCodeNode.SetAttribute("Value", "1033:{$newGuid}")
    } else {
        throw "Error: ProductCode node not found in Installer.aip."
    }

    $aipXml.Save($aipFilePath)
}

function Update-AssemblyInfoFile {
    param (
        [string]$newVersion,
        [string]$assemblyInfoFilePath
    )

    if ([string]::IsNullOrEmpty($assemblyInfoFilePath)) {
        throw "AssemblyInfo.cs file path is empty or null."
    }

    # Read all lines from AssemblyInfo.cs
    $assemblyInfoLines = Get-Content -Path $assemblyInfoFilePath -Encoding UTF8

    # Loop through each line and replace AssemblyVersion and AssemblyFileVersion
    for ($i = 0; $i -lt $assemblyInfoLines.Length; $i++) {
        if ($assemblyInfoLines[$i] -match 'AssemblyVersion') {
            $assemblyInfoLines[$i] = "[assembly: AssemblyVersion(`"$newVersion`")]"
        }
        if ($assemblyInfoLines[$i] -match 'AssemblyFileVersion') {
            $assemblyInfoLines[$i] = "[assembly: AssemblyFileVersion(`"$newVersion`")]"
        }
    }

    # Output the updated AssemblyInfo content to verify
    Write-Output "Updated AssemblyInfo content:"
    $assemblyInfoLines | ForEach-Object { Write-Output $_ }

    # Write the modified content back to the file
    Set-Content -Path $assemblyInfoFilePath -Value $assemblyInfoLines -Encoding UTF8
}

$currentVersionData = Get-CurrentVersion -filePath $SettingsFile
$currentVersion = $currentVersionData.Version

$newVersion = Increment-Version -version $currentVersion

Update-CsprojVersion -newVersion $newVersion -csprojFilePath $CsprojFilePath
Update-SettingsFile -newVersion $newVersion -settingsFilePath $SettingsFile
Update-VersionTxtFile -newVersion $newVersion -versionTxtFilePath $VersionTxtFilePath
Update-AppConfig -newVersion $newVersion -appConfigFilePath $AppConfigFilePath
Update-AutoUpdaterXml -newVersion $newVersion -xmlOutputPath $XmlOutputPath
Update-AssemblyInfoFile -newVersion $newVersion -assemblyInfoFilePath $AssemblyInfoFilePath
Update-AipFile -newVersion $newVersion -aipFilePath $AipFilePath

Write-Output "Version incremented to $newVersion"
