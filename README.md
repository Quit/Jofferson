Jofferson
======

Jofferson is a tool designed to aid Stonehearth mod creators. It parses all mods in its current directory (or, if present, the `mods` directory) and creates a list of mods and their references. It is capable of detecting broken references, aliases and invalid JSON and it can even, to some crude extent, show what a file looks like after all files have been mixinto'd.

This was my first attempt at WPF so the code is a bit inconsistent in its design. Jofferson is more or less complete and will receive no major feature updates. I have some ideas about a possible successor that would be written much nicer and featured more features, possibly by merging it together with Panicle to offer a model viewer and animation previewer as well.

## Screenshots
![The main window](http://i.imgur.com/Mx9dT6Z.png)
![The resource window](http://i.imgur.com/xfP5RNj.png)
![The JSON viewer](http://i.imgur.com/nScI8nc.png)

## Compiling requirements and 3rd party libraries
Jofferson was written in Visual Studio 2013 and requires .NET 4.5.1 as well as WPF, which should already be installed on Vista and higher. Jofferson uses [JSON.NET](http://james.newtonking.com/json) which should be downloaded using nuget the first time you try to compile it.

## License
Jofferson is licensed under the MIT license. See LICENSE for details.