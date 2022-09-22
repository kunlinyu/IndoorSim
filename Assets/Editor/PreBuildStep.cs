// TODO(kunlin): I try to enable WebGL.threadsSupport according to https://medium.com/medialesson/so-you-want-to-use-multithreading-in-unity-webgl-5953769dd337
//               But I can not compile well.

// using UnityEngine;
// using UnityEditor;

// [InitializeOnLoad]
// public class PreBuildStep
// {
//     static PreBuildStep()
//     {
//         PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
//         PlayerSettings.WebGL.emscriptenArgs = "-s ALLOW_MEMORY_GROWTH=1";
//         PlayerSettings.WebGL.threadsSupport = true;
//         PlayerSettings.WebGL.memorySize = 2032;
//     }
// }
