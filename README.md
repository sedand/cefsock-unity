# cefsock-unity

> Embed a chromium browser in a unity scene.

CEF ([Chromium Embedded Framework](https://bitbucket.org/chromiumembedded/cef/)) offscreen application pushing it's framebuffer over a socket (to be read by unity).   
This allows to show a live browser inside a unity scene.   

## Goals

- Cross Platform (Linux/Windows)
- No extra dependencies
- Cef invisible to the end user
- Currently: View-only (no input fed back from unity to the browser)

## TODO

- Make cefsock configurable, currently `cefsock.cc` must be modified to change URL etc.
- Windows version

## Considerations
- Use a socket (not a named pipe as does [chromium-unity-server](https://github.com/roydejong/chromium-unity-server)) to communicate between CEF and unity
- No additional wrappers (no CefGlue, no .NET bindings etc.)
- Cef & unity could also be run on separate hosts (untested), as communication is done over a network socket
- Currently only tested/developed for Linux, but Windows support is in progress and should be easy
- Inspired by and thanks to:
   - [chromium-unity-server](https://github.com/roydejong/chromium-unity-server)
   - [CefUnitySample](https://github.com/aleab/cef-unity-sample)

Based on cef sample project [cef-project](https://bitbucket.org/chromiumembedded/cef-project)

## Build, deploy/integrate in unity, configure

0. Modify `cefsock.cc`: Change URL, browser window size etc.
1. Compile the cefsock binary (automatically downloads CEF). See README in folder `cef`.
2. After compilation, copy the application folder `build/cefsock/Release` to the root of your unity project folder and rename it to `cefsock`.
3. Copy the unity script ../unity/Cefsock-unity.cs to your unity project's Script folder.
4. Add the CefSockUnity script to a RawImage gameobject. Configure the options. If you did not rename the folder to `cefsock` adjust the  `Cef Folder Name` script parameter to match the application foldername.
5. After building your unity project, copy the `cefsock` folder to the build folder.

## Note

Some parts are still pretty rough, use at your own risk!