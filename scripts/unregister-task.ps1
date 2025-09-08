Param(
    [string]$TaskName = 'CopyPolish Elevated'
)

$ErrorActionPreference = 'Stop'

if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "Scheduled Task kaldırıldı: $TaskName" -ForegroundColor Yellow
} else {
    Write-Host "Scheduled Task bulunamadı: $TaskName" -ForegroundColor Yellow
}
