using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ETModel
{
	[ObjectSystem]
	public class UiBundleDownloaderComponentAwakeSystem : AwakeSystem<BundleDownloaderComponent>
	{
		public override void Awake(BundleDownloaderComponent self)
		{
			self.bundles = new Queue<string>();
			self.downloadedBundles = new HashSet<string>();
			self.downloadingBundle = "";
		}
	}

	/// <summary>
	/// 用来对比web端的资源，比较md5，对比下载资源
	/// </summary>
	public class BundleDownloaderComponent : Component
	{
		private VersionConfig remoteVersionConfig;
		
		public Queue<string> bundles;

		public long TotalSize;

		public HashSet<string> downloadedBundles;

		public string downloadingBundle;

		public UnityWebRequestAsync webRequest;

		public async ETTask StartAsync()
		{
			// 获取远程的Version.txt
			string versionUrl = "";
			try
			{
				using (UnityWebRequestAsync webRequestAsync = ComponentFactory.Create<UnityWebRequestAsync>())
				{
                    //UnityWebRequest UnityWebRequest www下载
                    versionUrl = GlobalConfigComponent.Instance.GlobalProto.GetUrl() + "StreamingAssets/" + "Version.txt";
					//Log.Debug(versionUrl);
					await webRequestAsync.DownloadAsync(versionUrl);
					remoteVersionConfig = JsonHelper.FromJson<VersionConfig>(webRequestAsync.Request.downloadHandler.text);
					//Log.Debug(JsonHelper.ToJson(this.VersionConfig));
				}

			}
			catch (Exception e)
			{
				throw new Exception($"url: {versionUrl}", e);
			}

			// 获取streaming目录的Version.txt
			VersionConfig streamingVersionConfig;
			string versionPath = Path.Combine(PathHelper.AppResPath4Web, "Version.txt");

			using (UnityWebRequestAsync request = ComponentFactory.Create<UnityWebRequestAsync>())
			{
				await request.DownloadAsync(versionPath);
				streamingVersionConfig = JsonHelper.FromJson<VersionConfig>(request.Request.downloadHandler.text);
			}
			
			// 删掉远程不存在的文件
			DirectoryInfo directoryInfo = new DirectoryInfo(PathHelper.AppHotfixResPath);
            //如果存放热更新资源的文件夹存在
			if (directoryInfo.Exists)
			{
                //获取到里面的所有子文件
				FileInfo[] fileInfos = directoryInfo.GetFiles();
				foreach (FileInfo fileInfo in fileInfos)
				{
                    //就是远程下载下来的版本文件 json反序列化成一个实体类 FileInfoDict这个字典就包含了所有AB文件信息
                    if (remoteVersionConfig.FileInfoDict.ContainsKey(fileInfo.Name))
					{
						continue;
					}

					if (fileInfo.Name == "Version.txt")
					{
						continue;
					}
					//为什么要删除掉远程不存在的文件呢 主要是为了避免加载资源出错 资源重复 会导致到报错
					fileInfo.Delete();
				}
			}
            //如果保存热更新资源的文件夹不存在的话 通过Create进行创建
            else
            {
				directoryInfo.Create();
			}

			// 对比MD5
			foreach (FileVersionInfo fileVersionInfo in remoteVersionConfig.FileInfoDict.Values)
			{
				// 对比md5 跟我们本地的文件进行MD5对比
				string localFileMD5 = BundleHelper.GetBundleMD5(streamingVersionConfig, fileVersionInfo.File);
                //如果相等 就忽略 表示两个版本中 这个文件并未做任何改动
				if (fileVersionInfo.MD5 == localFileMD5)
				{
					continue;
				}
                //如果两个文件的MD5不一致 把要下载的文件 压入到队列
				this.bundles.Enqueue(fileVersionInfo.File);
                //下载的总大小也加上这个文件的大小
				this.TotalSize += fileVersionInfo.Size;
			}
		}

        //下载进度0%-100%
		public int Progress
		{
			get
			{
				if (this.TotalSize == 0)
				{
					return 0;
				}

				long alreadyDownloadBytes = 0;
                //已经下载的文件大小
				foreach (string downloadedBundle in this.downloadedBundles)
				{
					long size = this.remoteVersionConfig.FileInfoDict[downloadedBundle].Size;
					alreadyDownloadBytes += size;
				}
                //包括正在下载的文件大小
				if (this.webRequest != null)
				{
					alreadyDownloadBytes += (long)this.webRequest.Request.downloadedBytes;
				}
                //当前下载大小=已经下载的+当前下载中的
                //当前下载的大小/总的大小=下载进度
				return (int)(alreadyDownloadBytes * 100f / this.TotalSize);
			}
		}

		public async ETTask DownloadAsync()
		{
			if (this.bundles.Count == 0 && this.downloadingBundle == "")
			{
				return;
			}

			try
			{
				while (true)
				{
					if (this.bundles.Count == 0)
					{
						break;
					}

                    //从队列 出列 获取到要下载的文件
					this.downloadingBundle = this.bundles.Dequeue();

					while (true)
					{
						try
						{
                            //UnityWebRequest进行下载请求
                            using (this.webRequest = ComponentFactory.Create<UnityWebRequestAsync>())
							{
                                //要下载的路径:GlobalConfigComponent.Instance.GlobalProto.GetUrl() + "StreamingAssets/" + this.downloadingBundle
                                await this.webRequest.DownloadAsync(GlobalConfigComponent.Instance.GlobalProto.GetUrl() + "StreamingAssets/" + this.downloadingBundle);
								byte[] data = this.webRequest.Request.downloadHandler.data;

                                //要写入本地文件夹的路径
								string path = Path.Combine(PathHelper.AppHotfixResPath, this.downloadingBundle);
                                using (FileStream fs = new FileStream(path, FileMode.Create))
								{
									fs.Write(data, 0, data.Length);
								}
							}
						}
						catch (Exception e)
						{
							Log.Error($"download bundle error: {this.downloadingBundle}\n{e}");
							continue;
						}

						break;
					}
                    //每下载一个 就会添加到哈希表中
					this.downloadedBundles.Add(this.downloadingBundle);
					this.downloadingBundle = "";
					this.webRequest = null;
				}
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}
	}
}
