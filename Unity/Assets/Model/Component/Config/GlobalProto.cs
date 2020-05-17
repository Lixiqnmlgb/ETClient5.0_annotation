namespace ETModel
{
	public class GlobalProto
	{
		//资源路径
		public string AssetBundleServerUrl;
		//服务器地址
		public string Address;

		//获取资源路径
		public string GetUrl()
		{
			string url = this.AssetBundleServerUrl;
#if UNITY_ANDROID
			url += "Android/";
#elif UNITY_IOS
			url += "IOS/";
#elif UNITY_WEBGL
			url += "WebGL/";
#elif UNITY_STANDALONE_OSX
			url += "MacOS/";
#else
			url += "PC/";
#endif
			Log.Debug(url);
			return url;
		}
	}
}
