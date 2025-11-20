$exePath = "$PSScriptRoot\ResidentPCBApp.exe"
$taskName = "ResidentPCBApp"

if (Test-Path $exePath) {
    schtasks /Create /TN $taskName /TR "`"$exePath`"" /SC ONLOGON /RL HIGHEST /F
    Write-Host "Task '$taskName' installed successfully!"
} else {
    Write-Host "Error: Executable not found at $exePath"
}
