#Run StarBreaker.Cli diff with the correct parameters
$starBreakerCliCsproj = "C:\Development\StarCitizen\StarBreaker\src\StarBreaker.Cli\StarBreaker.Cli.csproj"
$starCitizenBasePath = "C:\Program Files\Roberts Space Industries\StarCitizen"

#$channel = "LIVE"
#$channel = "PTU"
#$channel = "4.0_PREVIEW"
$channel = "HOTFIX"

$starCitizenPath = "$starCitizenBasePath\$channel"
$gitRepoPath = "C:\Development\StarCitizen\StarCitizenDiff2"

dotnet run --project $starBreakerCliCsproj -- diff -g $starCitizenPath -o $gitRepoPath -k false -f json

#Set-Location $gitRepoPath
#git add .
#git commit --author="StarBreaker <>" -m "StarBreaker diff"
##
#Write-Host "Diff completed"