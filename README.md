# cefsock-unity

> Embed a chromium browser in a unity scene.

CEF ([Chromium Embedded Framework](https://bitbucket.org/chromiumembedded/cef/)) offscreen application pushing it's framebuffer over a socket (to be read by unity).   
This allows to show a live browser inside a unity scene.   

## Goals

- Cross Platform (Linux/Windows)
- No extra dependencies
- Cef invisible to the end user
- Currently: View-only (no input fed back from unity to the browser)

## Considerations
- Use a socket (and not a named pipe as does [chromium-unity-server](https://github.com/roydejong/chromium-unity-server)) to communicate between CEF and unity, so it is easily cross-platform compatible
- No additional wrappers (no CefGlue, no .NET bindings etc.)
- Cef & unity could also be run on separate hosts (untested), as communication is done over a network socket
- Currently only tested/developed for Linux, but Windows support is in progress and should be easy
- Inspired by and thanks to:
   - [chromium-unity-server](https://github.com/roydejong/chromium-unity-server)
   - [CefUnitySample](https://github.com/aleab/cef-unity-sample)

Based on cef sample project [cef-project](https://bitbucket.org/chromiumembedded/cef-project)

## Overview

Setup requires the following 3 basic steps:
1. Compile the cefpipe binary (automatically downloads CEF)
2. Copy the binary and unity script to your unity project
3. Configure the script in unity

For 1. see README in folder `cef`

## Deploy/Integrate in unity

- After compilation, copy the application folder `build/cefpipe/Release` to the root of your unity project folder (optional: Rename it to `cefsock-unity`)
- Copy the unity script ../unity/Cefsock-unity.cs to your unity project's Script folder.
- Adjust the Cefpipe application path via the Unity GUI on the CefPipe script to match the application folder (If you did not rename the folder), so that unity is able to find the executable.