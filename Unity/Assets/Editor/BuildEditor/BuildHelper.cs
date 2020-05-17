using System.IO;
using ETModel;
using UnityEditor;

namespace ETEditor
{
	public static class BuildHelper
	{
		private const string relativeDirPrefix = "../Release";

		public static string BuildFolder = "../Release/{0}/StreamingAssets/";
		
		
		[MenuItem("Tools/web资源服务器")]
		public static void OpenFileServer()
		{
            //进程帮助类 dotnet
            ProcessHelper.Run("dotnet", "FileServer.dll", "../FileServer/");
		}

		public static void Build(PlatformType type, BuildAssetBundleOptions buildAssetBundleOptions, BuildOptions buildOptions, bool isBuildExe, bool isContainAB)
		{
			BuildTarget buildTarget = BuildTarget.StandaloneWindows;
			string exeName = "ET";
			switch (type)
			{
				case PlatformType.PC:
					buildTarget = BuildTarget.StandaloneWindows64;
					exeName += ".exe";
					break;
				case PlatformType.Android:
					buildTarget = BuildTarget.Android;
					exeName += ".apk";
					break;
				case PlatformType.IOS:
					buildTarget = BuildTarget.iOS;
					break;
				case PlatformType.MacOS:
					buildTarget = BuildTarget.StandaloneOSX;
					break;
			}

			string fold = string.Format(BuildFolder, type);
			if (!Directory.Exists(fold))
			{
				Directory.CreateDirectory(fold);
			}
			
			Log.Info("开始资源打包");
			BuildPipeline.BuildAssetBundles(fold, buildAssetBundleOptions, buildTarget);
			
            //生成Version.txt
			GenerateVersionInfo(fold);
			Log.Info("完成资源打包");

			if (isContainAB)
			{
				FileHelper.CleanDirectory("Assets/StreamingAssets/");
				FileHelper.CopyDirectory(fold, "Assets/StreamingAssets/");
			}

			if (isBuildExe)
			{
				AssetDatabase.Refresh();
				string[] levels = {
					"Assets/Scenes/Init.unity",
				};
				Log.Info("开始EXE打包");
				BuildPipeline.BuildPlayer(levels, $"{relativeDirPrefix}/{exeName}", buildTarget, buildOptions);
				Log.Info("完成exe打包");
			}
		}

		private static void GenerateVersionInfo(string dir)
		{
            //将所有的AB文件写入到 FileInfoDict 这个字典中
            VersionConfig versionProto = new VersionConfig();
			GenerateVersionProto(dir, versionProto, "");

            //创建一个文件流 然后往这个流里写入数据 
			using (FileStream fileStream = new FileStream($"{dir}/Version.txt", FileMode.Create))
			{
                //序列化成byte[]
				byte[] bytes = JsonHelper.ToJson(versionProto).ToByteArray();
                //通过字节数组写入到文本文件中
				fileStream.Write(bytes, 0, bytes.Length);
			}
		}

		private static void GenerateVersionProto(string dir, VersionConfig versionProto, string relativePath)
		{
            //遍历输出AB包的路径 找到他下面的所有资源(文件)
			foreach (string file in Directory.GetFiles(dir))
			{
                //每个文件信息:Md5 获取它的大小 路径
                string md5 = MD5Helper.FileMD5(file);
				FileInfo fi = new FileInfo(file);
				long size = fi.Length;
				string filePath = relativePath == "" ? fi.Name : $"{relativePath}/{fi.Name}";

                //key是文件的路径 Value文件信息
				versionProto.FileInfoDict.Add(filePath, new FileVersionInfo
				{
					File = filePath,
					MD5 = md5,
					Size = size,
				});
			}

            //对资源AB包输出路径下的文件夹进行操作
			foreach (string directory in Directory.GetDirectories(dir))
			{
                //找到子文件夹 然后回调GenerateVersionProto 将文件夹路径传递进来
                DirectoryInfo dinfo = new DirectoryInfo(directory);
				string rel = relativePath == "" ? dinfo.Name : $"{relativePath}/{dinfo.Name}";
				GenerateVersionProto($"{dir}/{dinfo.Name}", versionProto, rel);
			}
		}
	}
}
