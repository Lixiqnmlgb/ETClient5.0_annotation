using System;
using System.Collections.Generic;
using System.Linq;

namespace ETModel
{
	public abstract class ACategory : Object
	{
		public abstract Type ConfigType { get; }
		public abstract IConfig GetOne();
		public abstract IConfig[] GetAll();
		public abstract IConfig TryGet(int type);
	}

	/// <summary>
	/// 管理该所有的配置
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class ACategory<T> : ACategory where T : IConfig
	{
		protected Dictionary<long, IConfig> dict;

		public override void BeginInit()
		{
			//创建字典 key是ID Value存储实体
			this.dict = new Dictionary<long, IConfig>();
			//获取配置文件的文本内容
			string configStr = ConfigHelper.GetText(typeof(T).Name);
			//根据换行符 进行切割 得到每一行 都是json格式
			foreach (string str in configStr.Split(new[] { "\n" }, StringSplitOptions.None))
			{
				try
				{
					string str2 = str.Trim();
					if (str2 == "")
					{
						continue;
					}
					//将json转化为实体
					T t = ConfigHelper.ToObject<T>(str2);
					//缓存起来
					this.dict.Add(t.Id, t);
				}
				catch (Exception e)
				{
					throw new Exception($"parser json fail: {str}", e);
				}
			}
		}

		public override Type ConfigType
		{
			get
			{
				return typeof(T);
			}
		}

		public override void EndInit()
		{//空空如也
		}
		//根据ID获取
		public override IConfig TryGet(int type)
		{
			IConfig t;
			if (!this.dict.TryGetValue(type, out t))
			{
				return null;
			}
			return t;
		}
		//获取所有
		public override IConfig[] GetAll()
		{
			return this.dict.Values.ToArray();
		}
		//获取第一个
		public override IConfig GetOne()
		{
			return this.dict.Values.First();
		}
	}
}