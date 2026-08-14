$ErrorActionPreference = "Stop"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$root = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Path $dist -Force | Out-Null

& $csc /nologo /target:winexe /optimize+ `
    /out:"$dist\dsh-tray.exe" `
    /win32icon:"$root\tray\icon.ico" `
    /resource:"$root\tray\icon.ico,dshTrayIcon.ico" `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    "$PSScriptRoot\dsh-tray.cs"

if ($LASTEXITCODE -eq 0) {
    Write-Output "OK -> $dist\dsh-tray.exe ($([math]::Round((Get-Item "$dist\dsh-tray.exe").Length/1KB,1)) KB)"
} else {
    Write-Error "compile failed: $LASTEXITCODE"
}
