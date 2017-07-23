# Dependencies - An open-source modern Dependency Walker

<p align="center">
<img alt="Usage Exemple" src="screenshots/UsageExemple.gif"/>
</p>


## Overview
`Dependencies` is a rewrite of the legacy software [Dependency Walker](http://www.dependencywalker.com/) which was shipped along Windows SDKs, but whose developement stopped around 2006.
`Dependencies` can help Windows developers troubleshooting their dll load dependencies issues.

## Releases

* v1.0 -- Initial release

## Installation and Usage

`Dependencies` is currently shipped as a binary (no installer present). Just uncompress the archive and click on it.
Since the binary is not signed, `SmartScreen` might scream at runtime.

## Limitations

At the moment, `Dependencies` recreates features and "features" of `depends.exe`, which means :

* Only direct, forwarded and delay load dependencies are supported. Dynamic loading via `LoadLibrary` are not supported (and probably won't ever be).
* `Min-win` dlls are not propertly supported.
* There is no check between Api Imports and Exports for the moment, only dll presence is supported.
* No support of esoteric dll load paths (via `AppPaths` or `SxS` manifests entries)


## Credits and licensing

Special thanks to :

* [ProcessHacker2](https://github.com/processhacker2/processhacker) for :
  * `phlib`, which does the heavy lifting for processing PE informations.
  * `peview`, a powerful and lightweight PE informations viewer.
* [wpfmdi](http://wpfmdi.codeplex.com/) a C# library which recreate the [MDI programming model](https://en.wikipedia.org/wiki/Multiple_document_interface) in `WPF`
* [Thomas levesque's blog](https://www.thomaslevesque.com) which pretty much solved all my `WPF` programming issues. His [`AutoGridSort`](http://www.thomaslevesque.com/2009/08/04/wpf-automatically-sort-a-gridview-continued/) is used in this project 
