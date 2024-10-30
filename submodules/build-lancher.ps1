try
{
	Push-Location

	cd $PSScriptRoot

	cd ./deployment-tools

	git reset --hard

	$phPath = "./src/clickonce/launcher/ProcessHelper.cs"
    $ph = (Get-Content -Raw -Encoding utf8 $phPath).Trim()
	$ph = $ph -replace "UseShellExecute = false","UseShellExecute = false, CreateNoWindow = true"
	Set-Content -Value $ph -Encoding utf8 $phPath

	dotnet build ./src/clickonce/launcher/Launcher.csproj -c Release -v d

	Move-Item ./artifacts/bin/Launcher/Release/net45/Launcher.exe ../Launcher.exe -Force

	git reset --hard
}
finally
{
	Pop-Location
}