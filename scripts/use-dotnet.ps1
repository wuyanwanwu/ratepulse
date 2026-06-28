$dotnetRoot = "E:\DevTools\dotnet"
$env:DOTNET_ROOT = $dotnetRoot
$env:PATH = "$dotnetRoot;$env:PATH"
& "$dotnetRoot\dotnet.exe" --info
