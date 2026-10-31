$ErrorActionPreference = 'Stop'
$appPath = Join-Path $PSScriptRoot 'MathSymbolPad.exe'
if (!(Test-Path -LiteralPath $appPath)) { throw '未找到 MathSymbolPad.exe，请先完整解压。' }
$desktopPath = [Environment]::GetFolderPath('Desktop')
$linkPath = Join-Path $desktopPath 'Math Symbol Pad.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($linkPath)
$shortcut.TargetPath = $appPath
$shortcut.WorkingDirectory = $PSScriptRoot
$shortcut.IconLocation = $appPath + ',0'
$shortcut.Description = '悬浮输入数学符号与区间模板'
$shortcut.Save()
Write-Output $linkPath
