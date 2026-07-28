# 获取本机机器码（与 EssSimulator --machine-id 算法一致）
$ErrorActionPreference = "Stop"
$raw = ""
try {
  $raw = (Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Cryptography" -Name MachineGuid).MachineGuid
} catch {
  $raw = "$env:COMPUTERNAME|$env:USERNAME|Windows"
}
$raw = $raw.Trim().ToLowerInvariant()
$bytes = [System.Text.Encoding]::UTF8.GetBytes("EssSimulator|" + $raw)
$sha = [System.Security.Cryptography.SHA256]::Create()
$hash = $sha.ComputeHash($bytes)
$hex = -join ($hash[0..15] | ForEach-Object { $_.ToString("x2") })
Write-Output $hex
