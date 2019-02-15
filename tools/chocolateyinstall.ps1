$ErrorActionPreference = 'Stop';
 
$packageName= $env:ChocolateyPackageName
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = 'https://github.com/bbougot/Popcorn/releases/download/$($env:ChocolateyPackageVersion)/Popcorn.msi'
 
$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  fileType      = 'exe'
  url           = $url
 
  softwareName  = 'popcorn*'
   
  silentArgs   = '--silent'
  validExitCodes= @(0)
}
 
Install-ChocolateyPackage @packageArgs