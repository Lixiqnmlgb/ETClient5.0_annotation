using System;
using UnityEngine;

namespace ETModel
{
	public static class ConfigHelper
	{
        //获取配置文本
		public static string GetText(string key)
		{
			try
			{
                //这里指向了Bundles/Independent/Config预制件 传递参数UnitConfig或者BuffConfig
                //或者以后其他自定义的,即可访问他们身上挂载的配置文本
                GameObject config = (GameObject)Game.Scene.GetComponent<ResourcesComponent>().GetAsset("config.unity3d", "Config");
				string configStr = config.Get<TextAsset>(key).text;
				return configStr;
			}
			catch (Exception e)
			{
				throw new Exception($"load config file fail, key: {key}", e);
			}
		}

        //获取全局配置
        //从Resources文件夹下获取KV预制件,其身上挂载的GlobalProto引用指向了Res/Config/GlobalProto文本
        //暂时只包含了文件服务器URL的配置以及远程服务器的IP与端口
        public static string GetGlobal()
		{
			try
			{
				GameObject config = (GameObject)ResourcesHelper.Load("KV");
				string configStr = config.Get<TextAsset>("GlobalProto").text;
				return configStr;
			}
			catch (Exception e)
			{
				throw new Exception($"load global config file fail", e);
			}
		}

		public static T ToObject<T>(string str)
		{
			return JsonHelper.FromJson<T>(str);
		}
	}
}