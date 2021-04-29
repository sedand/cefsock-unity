CEF offscreen application pushing it's framebuffer over a namedpipe (to be read by unity).
Based on cef sample project "cef-project".

The [Chromium Embedded Framework](https://bitbucket.org/chromiumembedded/cef/) (CEF) is a simple framework for embedding Chromium-based browsers in other applications.

# Setup

First install some necessary tools.

1\. Install [CMake](https://cmake.org/), a cross-platform open-source build system. Version 2.8.12.1 or newer is required.

2\. Install [Python](https://www.python.org/downloads/). Version 2.7.x is required. If Python is not installed to the default location you can set the `PYTHON_EXECUTABLE` environment variable before running CMake (watch for errors during the CMake generation step below).

3\. Install platform-specific build tools.

* Linux: Currently supported distributions include Debian Wheezy, Ubuntu Precise, and related. Ubuntu 18.04 64-bit is recommended. Newer versions will likely also work but may not have been tested. Required packages include: build-essential, libgtk2.0-dev, libgtkglext1-dev.
* MacOS: Xcode 8 or newer building on MacOS 10.11 (El Capitan) or newer for x86_64. Xcode 12.2 or newer building on MacOS 10.15.4 (Catalina) or newer for ARM64. The Xcode command-line tools must also be installed. Only 64-bit builds are supported on macOS.
* Windows: Visual Studio 2015 Update 2 or newer building on Windows 7 or newer. Visual Studio 2019 and Windows 10 64-bit are recommended.

# Build

Now run CMake which will download the CEF binary distribution from the [Spotify automated builder](https://cef-builds.spotifycdn.com/index.html) and generate build files for your platform. Then build using platform build tools. For example, using the most recent tool versions on each platform:

```
# Create and enter the build directory.
mkdir build
cd build

# To perform a Linux build using a 32-bit CEF binary distribution on a 32-bit
# Linux platform or a 64-bit CEF binary distribution on a 64-bit Linux platform:
cmake -G "Unix Makefiles" -DCMAKE_BUILD_TYPE=Release ..
make -j4

# To perform a MacOS build using a 64-bit CEF binary distribution:
cmake -G "Xcode" -DPROJECT_ARCH="x86_64" ..
# Then, open build\cef.xcodeproj in Xcode and select Product > Build.

# To perform a MacOS build using an ARM64 CEF binary distribution:
cmake -G "Xcode" -DPROJECT_ARCH="arm64" ..
# Then, open build\cef.xcodeproj in Xcode and select Product > Build.

# To perform a Windows build using a 32-bit CEF binary distribution:
cmake -G "Visual Studio 16" -A Win32 ..
# Then, open build\cef.sln in Visual Studio 2019 and select Build > Build Solution.

# To perform a Windows build using a 64-bit CEF binary distribution:
cmake -G "Visual Studio 16" -A x64 ..
# Then, open build\cef.sln in Visual Studio 2019 and select Build > Build Solution.
```

CMake supports different generators on each platform. Run `cmake --help` to list all supported generators. Generators that have been tested with CEF include:

* Linux: Ninja, Unix Makefiles
* MacOS: Ninja, Xcode 8+ (x86_64) or Xcode 12.2+ (ARM64)
* Windows: Ninja, Visual Studio 2015+

Ninja is a cross-platform open-source tool for running fast builds using pre-installed platform toolchains (GNU, clang, Xcode or MSVC). See comments in the "third_party/cef/cef_binary_*/CMakeLists.txt" file for Ninja usage instructions.

# Next Steps

[TODO] copy compiled application to your unity project folder
[TODO] Add the unity script from ../unity/ to your project folder