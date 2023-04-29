```mermaid
classDiagram
    Shared_dll <|-- Engine_dll
    Shared_dll <|-- Equilibrium_dll_hot_reload
    Shared_dll: +Bgfx, Utils
    Shared_dll: +Components
    Shared_dll: +IPlugin, ISystems
    Shared_dll: 
    class Engine_dll{
      +Launcher
      +PluginManager
      +ShaderCompiler
      +SdlSystem
      +BgfxSystem
      +ImGuiSystem
    }
    class Equilibrium_dll_hot_reload{
      -IPlugin()
      -Game code
    }
```