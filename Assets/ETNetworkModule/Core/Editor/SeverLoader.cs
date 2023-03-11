using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SeverLoader {

// 这里最主要的问题是：它只提供了一个可执行的 .exe 文件，但是并不曾提供这个可执行的 .exe 是如何生成的，找不到源码
    [MenuItem("Tools/Start Test Server", priority = 0)]
    static void StarServer() {
        Process pr = new Process();
        pr.StartInfo.WorkingDirectory = Path.Combine(Application.dataPath, "..", "Server/exe");
        pr.StartInfo.FileName = "Server.exe";
        pr.Start();
    }
}
