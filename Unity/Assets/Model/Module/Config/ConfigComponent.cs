using System;
using System.Collections.Generic;

namespace ETModel
{
	[ObjectSystem]
	public class ConfigComponentAwakeSystem : AwakeSystem<ConfigComponent>
	{
		public override void Awake(ConfigComponent self)
		{
			Log.Debug("Awake加载....");
			self.Awake();
		}
	}

	[ObjectSystem]
	public class ConfigComponentLoadSystem : LoadSystem<ConfigComponent>
	{
		public override void Load(ConfigComponent self)
		{
			//Log.Debug("Load加载....");
			self.Load();
		}
	}

	/// <summary>
	/// Config组件会扫描所有的有ConfigAttribute标签的配置,加载进来
	/// </summary>
	public class ConfigComponent: Component
	{
		private Dictionary<Type, ACategory> allConfig = new Dictionary<Type, ACategory>();

		public void Awake()
		{
			this.Load();
		}
		public void Load()
		{
			this.allConfig.Clear();
			//先获取到所有加了ConfigAttribute的类
			List<Type> types = Game.EventSystem.GetTypes(typeof(ConfigAttribute));
			foreach (Type type in types)
			{
				object[] attrs = type.GetCustomAttributes(typeof (ConfigAttribute), false);
				if (attrs.Length == 0)
				{
					continue;
				}
				
				ConfigAttribute configAttribute = attrs[0] as ConfigAttribute;
				// 如果不是客户端的配置 遍历下一个元素
				if (!configAttribute.Type.Is(AppType.ClientM))
				{
					continue;
				}
				object obj = Activator.CreateInstance(type);//创建实例
				ACategory iCategory = obj as ACategory;//转换类型 如果成功 不会等于null
				if (iCategory == null)
				{
					throw new Exception($"class: {type.Name} not inherit from ACategory");
				}
				iCategory.BeginInit();//调用实例开始初始化的方法
				iCategory.EndInit();
				this.allConfig[iCategory.ConfigType] = iCategory;//加到缓存里
			}
		}
		//根据类型 获取其缓存的配置的第一条记录
		public IConfig GetOne(Type type)
		{
			ACategory configCategory;
			if (!this.allConfig.TryGetValue(type, out configCategory))
			{
				throw new Exception($"ConfigComponent not found key: {type.FullName}");
			}
			return configCategory.GetOne();
		}
		//根据类型 获取其缓存的配置 再根据ID获取对应的记录
		public IConfig Get(Type type, int id)
		{
			ACategory configCategory;
			if (!this.allConfig.TryGetValue(type, out configCategory))
			{
				throw new Exception($"ConfigComponent not found key: {type.FullName}");
			}

			return configCategory.TryGet(id);
		}
		//根据类型 获取其缓存的配置 再根据ID获取对应的记录
		public IConfig TryGet(Type type, int id)
		{
			ACategory configCategory;
			if (!this.allConfig.TryGetValue(type, out configCategory))
			{
				return null;
			}
			return configCategory.TryGet(id);
		}

		public IConfig[] GetAll(Type type)
		{
			ACategory configCategory;
			if (!this.allConfig.TryGetValue(type, out configCategory))
			{
				throw new Exception($"ConfigComponent not found key: {type.FullName}");
			}
			return configCategory.GetAll();
		}

		public ACategory GetCategory(Type type)
		{
			ACategory configCategory;
			bool ret = this.allConfig.TryGetValue(type, out configCategory);
			return ret ? configCategory : null;
		}
	}
}