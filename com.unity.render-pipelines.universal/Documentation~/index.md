# About the Universal Render Pipeline

![Universal Render Pipeline in action](Images/AssetShots/Beauty/Overview.png)

The Universal Render Pipeline (Universal RP) is a prebuilt Scriptable Render Pipeline, made by Unity. The technology offers graphics that are scalable to mobile platforms, and you can also use it for higher-end consoles and PCs. You’re able to achieve quick rendering at a high quality without needing compute shader technology. Universal RP uses simplified, physically based Lighting and Materials.

The Universal RP uses single-pass forward rendering. Use this pipeline to get optimized real-time performance on several platforms. 

The Universal RP is supported on the following platforms:
* Windows and UWP
* Mac and iOS
* Android
* XBox One
* PlayStation4
* Nintendo Switch
* WebGL
* All current VR platforms

The Universal Render Pipeline is available via two templates: Universal RP and Universal RP-VR. The  Universal RP-VR comes with pre-enabled settings specifically for VR. The documentation for both render pipelines is the same. For any questions regarding Universal RP-VR, see the Universal RP documentation.

**Note:**  Built-in and custom Lit Shaders do not work with the Universal Render Pipeline. Instead, Universal RP has a new set of standard Shaders. If you upgrade a current Project to Universal RP, you can [upgrade built-in Shaders to the Universal RP ones](upgrading-your-shaders.md).

**Note:** Projects made using Universal RP are not compatible with the High Definition Render Pipeline or the Built-in Unity render¢pipeline. Before you start development, you must decide which render pipeline to use in your Project. 