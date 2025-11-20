$taskName = "ResidentPCBApp"
schtasks /Delete /TN $taskName /F
Write-Host "Task '$taskName' removed."
