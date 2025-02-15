using ET;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class HandlerGenerator {
    // 生成 Handler 保存的位置
    static string path = $"{Application.dataPath}/ETNetworkModule/Generated/Handlers";
    [MenuItem("Tools/生成非RPC消息处理器")]
    static void Generate() { 
        var messages = AppDomain.CurrentDomain.GetAssemblies()
            .Where(v => v.FullName.StartsWith("com.network.generated")) // 只处理 com.network.generated 程序集的消息
            .SelectMany(assembly => assembly.GetTypes())
            .Where(v => v.IsClass)
            .Where(v => typeof(IMessage).IsAssignableFrom(v) && !typeof(IRequest).IsAssignableFrom(v) && !typeof(IResponse).IsAssignableFrom(v))
            .ToList(); 
        if (messages.Count > 0) {
            TryCreateAssemblyDefinitionFile();
            count = 0;
            messages.ForEach(GenerateCode);
            Debug.Log($"{nameof(HandlerGenerator)}: {(count == 0 ? "Handler 无新增" : $"生成 Handler {count}个")}，操作完成！");
            if (count > 0) {
                AssetDatabase.Refresh();
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<DefaultAsset>(FileUtil.GetProjectRelativePath(path)));
            }
        }
    }
    static int count;
    static void GenerateCode(Type message) {
        var dirInfo = GetSaveLocation();
        var type = message.Name;
        var name = $"{type}Handler";
        var file = Path.Combine(dirInfo.FullName, $"{name}.cs"); // 这里就包括了：文件目录和文件名
        if (!File.Exists(file)) { // 这里的生成，同样是写死的，全都继承自 AMHandler<>
            var content = @$"namespace ET {{
            [MessageHandler]
            public class {name} : AMHandler<{type}> {{}}
            }}";
            File.WriteAllText(file, content, System.Text.Encoding.UTF8);
            Debug.Log($"{nameof(HandlerGenerator)}: 生成 {name} 成功！");
            count++;
        }
    }
    private static DirectoryInfo GetSaveLocation() {
        var dirInfo = new DirectoryInfo(path);
        if (!dirInfo.Exists) {
            dirInfo.Create();
        }
        return dirInfo;
    }
    // 为降低反射遍历消息的次数、减小编译时长，故使用 AssemblyDefinition 
    private static void TryCreateAssemblyDefinitionFile() { // 感觉这个方法，是固定的写死的？不是说动态生成？为什么 GUID 会是固定的字符串？
        string file = "com.network.generated.asmdef";
        string content = @"{
            ""name"": ""com.network.generated"",
            ""references"": [ 
                ""GUID:97baa7ef701375d4992b10159aec3da7"",
                ""GUID:348e1548f7bc88348b10043acbbf70df""
                ],
            ""autoReferenced"": true
            }";
            var path = Path.Combine(GetSaveLocation().FullName, "../", file);
        if (!File.Exists(path)) {
            File.WriteAllText(path, content, System.Text.Encoding.UTF8);
            Debug.Log($"{nameof(HandlerGenerator)}: Assembly Definition File 生成 {file} 成功！");
        }
    }
}