<p align="center">
  <img src="https://raw.githubusercontent.com/bbougot/Popcorn/master/Popcorn/icon.ico" alt="Logo" />
</p>

# Popcorn

[![Gitter](https://img.shields.io/badge/Gitter-Join%20Chat-green.svg?style=flat-square)](https://gitter.im/popcorn-app/popcorn)
<a href="https://popcorn-slack.azurewebsites.net" target="_blank">
  <img alt="Slack" src="http://popcorn-slack.azurewebsites.net/badge.svg">
</a> 

[![Build status](https://ci.appveyor.com/api/projects/status/mjnfwck6otg9c5wj/branch/master?svg=true)](https://ci.appveyor.com/project/bbougot/popcorn/branch/master) 
[![Quality Gate](https://sonarqube.com/api/badges/gate?key=Popcorn)](https://sonarqube.com/dashboard/index/Popcorn)
[![Coverage Status](https://coveralls.io/repos/github/bbougot/Popcorn/badge.svg?branch=master)](https://coveralls.io/github/bbougot/Popcorn?branch=master) 

An application which aims to provide a simple interface to watch any movie.

![Screenshot1](https://github.com/bbougot/Popcorn/blob/master/Screenshots/Screen1.jpg)

![Screenshot2](https://github.com/bbougot/Popcorn/blob/master/Screenshots/Screen2.jpg)

![Screenshot3](https://github.com/bbougot/Popcorn/blob/master/Screenshots/Screen3.jpg)

![Screenshot5](https://github.com/bbougot/Popcorn/blob/master/Screenshots/Screen4.jpg)

![Screenshot4](https://github.com/bbougot/Popcorn/blob/master/Screenshots/Screen5.jpg)

## What does it use?
#### Backend
.NET Framework 4.6.2 and C# 7
#### UI
WPF/XAML
#### IDE
Visual Studio 2017

### Dependencies
* MVVM framework: [MVVM Light](https://mvvmlight.codeplex.com) 
* UI framework: [MahApps](https://github.com/MahApps/MahApps.Metro)
* Media Player: [Meta.Vlc](https://github.com/higankanshi/Meta.Vlc)
* Torrent : [libtorrent-net](https://github.com/bbougot/libtorrent-net)
* JSON Deserialization: [Json.NET](https://github.com/JamesNK/Newtonsoft.Json)
* REST management: [RestSharp](https://github.com/restsharp/RestSharp)
* Logging: [NLog](https://github.com/NLog/NLog) and [Application Insights](https://github.com/Microsoft/ApplicationInsights-dotnet)
* Unit testing: [NUnit](https://github.com/nunit/nunit) & [AutoFixture](https://github.com/AutoFixture/AutoFixture)
* IMDb data: [TMDbLib](https://github.com/LordMike/TMDbLib/)
* Localization: [WpfLocalizeExtension](https://github.com/SeriousM/WPFLocalizationExtension)

### API
* Using own private [API](https://popcornapi.azurewebsites.net/), hosted in Azure as a ASP.NET Core web app (source [here](https://github.com/bbougot/PopcornApi)).

## Supported platforms
At this time, only Windows 7+ is supported (Windows 7, 8, 8.1, 10).

## Can I help you?
Of course yes! Any pull-request will be considered.

## Installer
Download full installer [here](https://github.com/bbougot/Popcorn/releases/download/v2.2.0/PopcornInstaller.exe)

## History version
See history [here](https://github.com/bbougot/Popcorn/blob/master/Popcorn/Markdown/Versions.md)
