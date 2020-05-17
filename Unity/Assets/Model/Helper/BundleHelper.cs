using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace ETModel
{
	public static class BundleHelper
	{
       
        public static async ETTask DownloadBundle()
		{
			if (Define.IsAsync)
			{
				try
				{
					using (BundleDownloaderComponent bundleDownloaderComponent = Game.Scene.
						AddComponent<BundleDownloaderComponent>())
					{
                        //跟远程对比 确认哪些文件需要进行更新 压入待更新的队列中
						await bundleDownloaderComponent.StartAsync();
						
                        //派发进行开始加载的事件
						Game.EventSystem.Run(EventIdType.LoadingBegin);
						
                        //就是从队列里取出要下载的文件信息 然后进行下载
						await bundleDownloaderComponent.DownloadAsync();
					}
					
					Game.EventSystem.Run(EventIdType.LoadingFinish);
					//以往记录所有AB信息的AB包 都是以平台名称定义的 那是因为父目录是以平台名称进行定义
					//而ET中,AB文件存放的父目录是StreamingAssets 而非平台名称 
					//所以无论什么平台,记录所有AB信息的文件都叫StreamingAssets
					Game.Scene.GetComponent<ResourcesComponent>().LoadOneBundle("StreamingAssets");
                    //缓存分析依赖的Mainifest文件
					ResourcesComponent.AssetBundleManifestObject = (AssetBundleManifest)Game.Scene
						.GetComponent<ResourcesComponent>().GetAsset("StreamingAssets", "AssetBundleManifest");
				}
				catch (Exception e)
				{
					Log.Error(e);
				}

			}
		}

		public static string GetBundleMD5(VersionConfig streamingVersionConfig, string bundleName)
		{
			string path = Path.Combine(PathHelper.AppHotfixResPath, bundleName);
			if (File.Exists(path))
			{
				return MD5Helper.FileMD5(path);
			}
			
			if (streamingVersionConfig.FileInfoDict.ContainsKey(bundleName))
			{
				return streamingVersionConfig.FileInfoDict[bundleName].MD5;	
			}

			return "";
		}
	}
}
