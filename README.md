# EquilibriumEngine-CSharp

Equilibrium Engine is a data-oriented **C#** game engine that takes advantage of **ECS** pattern followed by **Hot-Reloading** of your libraries and shaders which allows you to quickly iterate on different aspects of your projects.

<p align="center">
<img src="docs/home.png">
</p>

## Features
  * [Arch C# Entity Component System](https://github.com/genaray/Arch)
  * Forward Shading. [PBR & HDR Tonemapping](https://github.com/pezcode/Cluster)
  * [AssimpNet](https://bitbucket.org/Starnick/assimpnet/src/master/) model loading
  * **NET 7.0** API using **C++** libraries via PInvoke such as SDL, [BGFX](https://github.com/bkaradzic/bgfx) & [imgui](https://github.com/ocornut/imgui)

#### Hot-Reloading of scripts

https://user-images.githubusercontent.com/105135724/235317402-0e627dac-ebef-4d71-be15-945d80b81c03.mp4

#### Hot-Reloading of shaders

https://github.com/clibequilibrium/EquilibriumEngine-CSharp/assets/105135724/a88acfb0-2bcf-4ce5-8088-3062fe7b1ac8

#### Entity inspector

https://user-images.githubusercontent.com/105135724/235317410-f22d20b0-b4b6-4341-bb0d-43e91609e112.mp4

<p align="center">
<img src="docs/inspector.png">
</p>

## Usage
#### To get started on Windows
* [Get VSCode](https://code.visualstudio.com/)
* [Install C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
* ```code .```
* Open command pallete ```Ctrl+Shift+P``` and select ```.NET Restore All Projects```
* ```Hit F5 to start debugging```

*You might need to install [NET 7.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)*

## Known issues

[Precompiled bgfx binaries don't have BGFX_CONFIG_PROFILER flag set](https://github.com/clibequilibrium/EquilibriumEngine-CSharp/issues/2#issuecomment-2620265309)

## Screenshots

<p align="center">
<img src="docs/city.png">
</p>

<p align="center">
<img src="docs/room.png">
</p>

