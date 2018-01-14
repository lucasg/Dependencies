# Dependencies - An open-source modern Dependency Walker

<p align="center">
<img alt="Usage Exemple" src="screenshots/UsageExemple.gif"/>
</p>


## Overview
`Dependencies` is a rewrite of the legacy software [Dependency Walker](http://www.dependencywalker.com/) which was shipped along Windows SDKs, but whose developement stopped around 2006.
`Dependencies` can help Windows developers troubleshooting their dll load dependencies issues.

## Releases
* [v1.6](https://github.com/lucasg/Dependencies/releases/download/v1.6/Dependencies.zip) :
	* Add appx packaging
* [v1.5](https://github.com/lucasg/Dependencies/releases/download/v1.5/Dependencies.zip) :
	* Support of Sxs parsing
	* Support of api set schema parsing
	* API and Modules list can be filtered
* [v1.0](https://github.com/lucasg/Dependencies/releases/download/v1.0/Dependencies.zip) -- Initial release

NB : due to [limitations on /clr compilation](https://msdn.microsoft.com/en-us/library/ffkc918h.aspx), `Dependencies` needs [Visual C++  Redistributable](https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads) installed to run properly.

## Installation and Usage

`Dependencies` is currently shipped as a binary (no installer present). Just uncompress the archive and click on it.
Since the binary is not signed, `SmartScreen` might scream at runtime. `Dependencies` also bundle `ClrPhTester.exe`, a dumpbin-like executable used to test for non-regressions.

Since `v1.6`, Dependencies is also packaged as an appx package (the Windows equivalent of a `.deb` file) which can be installed locally. However, you also need to add the test certificate `DependenciesAppx_TemporaryKey.cer` to your cert hive.

`Dependencies` currently does not recursively resolve child imports when parsing a new PE since it can be really memory-hungry to do so ( it can over a GB even for "simple" PEs ). This behaviour can be overriden (app-wide) via a property located in "Options->Properties->Tree build behaviour".

<p align="center">
<img alt="User options" src="screenshots/UserOptions.png"/>
</p>

Tree build behaviours available :

* `ChildOnly` (default) : only process PE child imports and nothing beyond.
* `RecursiveOnlyOnDirectImports`  : do not process delayload dlls.
* `Recursive` : Full recursive analysis. You better have time and RAM on your hands if you activate this setting.


## Limitations

At the moment, `Dependencies` recreates features and "features" of `depends.exe`, which means :

* Only direct, forwarded and delay load dependencies are supported. Dynamic loading via `LoadLibrary` are not supported (and probably won't ever be).
* `Min-win` dlls are not propertly supported. (UPDATED : support of api set schema redirection in 1.5)
* There are no checks between Api Imports and Exports for the moment, only dll presence is supported.
* No support of esoteric dll load paths (via `AppPaths` or `SxS` manifests entries). (UPDATED : minimal support of sxs private manifests search).


## Credits and licensing

Special thanks to :

* [ProcessHacker2](https://github.com/processhacker2/processhacker) for :
  * `phlib`, which does the heavy lifting for processing PE informations.
  * `peview`, a powerful and lightweight PE informations viewer.
* [Dragablz](https://github.com/ButchersBoy/Dragablz) a C#/XAML library which implement dockable and dragable UI elements, and can recreate the [MDI programming model](https://en.wikipedia.org/wiki/Multiple_document_interface) in `WPF`.
* @aionescu, @zodiacon and Quarkslab for their public infos on ApiSets schema.
* [Thomas levesque's blog](https://www.thomaslevesque.com) which pretty much solved all my `WPF` programming issues. His [`AutoGridSort`](http://www.thomaslevesque.com/2009/08/04/wpf-automatically-sort-a-gridview-continued/) is used in this project 
