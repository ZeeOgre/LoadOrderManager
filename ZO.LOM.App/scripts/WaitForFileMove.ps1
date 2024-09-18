param (
    [string]$filePath,
    [int]$timeoutInSeconds = 10
)

$endTime = (Get-Date).AddSeconds($timeoutInSeconds)

while ((Get-Date) -lt $endTime) {
    if (Test-Path $filePath) {
        Write-Output "File found: $filePath"
        exit 0
    }
    Start-Sleep -Seconds 1
}

Write-Output "Timeout waiting for file: $filePath"
exit 1
