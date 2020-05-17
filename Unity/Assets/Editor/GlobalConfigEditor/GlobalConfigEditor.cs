using System.IO;
using ETModel;
using UnityEditor;
using UnityEngine;

namespace ETEditor
{
    public class GlobalProtoEditor: EditorWindow
    {
        const string path = @"./Assets/Res/Config/GlobalProto.txt";

        private GlobalProto globalProto;

        [MenuItem("Tools/全局配置")]
        public static void ShowWindow()
        {
            GetWindow<GlobalProtoEditor>();
        }

        public void Awake()
        {
            //如果包含GlobalProto.txt文件
            if (File.Exists(path))
            {
                //读取GlobalProto文本,反序列化成GlobalProto对象
                this.globalProto = JsonHelper.FromJson<GlobalProto>(File.ReadAllText(path));
            }
            else
            {
                this.globalProto = new GlobalProto();
            }
        }

        public void OnGUI()
        {
            //定义输入框 填写资源路径
            this.globalProto.AssetBundleServerUrl = EditorGUILayout.TextField("资源路径:", this.globalProto.AssetBundleServerUrl);
            //定义输入框 填写服务器地址
            this.globalProto.Address = EditorGUILayout.TextField("服务器地址:", this.globalProto.Address);

            if (GUILayout.Button("保存"))
            {//将globalProto序列化成json格式的字符串,然后写入到GlobalProto.txt中
                File.WriteAllText(path, JsonHelper.ToJson(this.globalProto));
                AssetDatabase.Refresh();
            }
        }
    }
}
