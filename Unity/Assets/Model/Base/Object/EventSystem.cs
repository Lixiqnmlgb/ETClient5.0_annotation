using System;
using System.Collections.Generic;
using System.Reflection;

namespace ETModel
{
	public enum DLLType
	{
		Model,
		Hotfix,
		Editor,
	}

	public sealed class EventSystem
	{
        //存储所有组件的字典
		private readonly Dictionary<long, Component> allComponents = new Dictionary<long, Component>();
        //存储程序集的字典
		private readonly Dictionary<DLLType, Assembly> assemblies = new Dictionary<DLLType, Assembly>();
        //无序的
		private readonly UnOrderMultiMap<Type, Type> types = new UnOrderMultiMap<Type, Type>();

		//实际存储所有继承自AEvent的类,因为AEvent继承自IEvent
		private readonly Dictionary<string, List<IEvent>> allEvents = new Dictionary<string, List<IEvent>>();

		//实际存储所有继承自AwakeSystem的类,因为AwakeSystem继承自IAwakeSystem
		private readonly UnOrderMultiMap<Type, IAwakeSystem> awakeSystems = new UnOrderMultiMap<Type, IAwakeSystem>();

		//实际存储所有继承自StartSystem的类,因为AwakeSystem继承自IStartSystem
		private readonly UnOrderMultiMap<Type, IStartSystem> startSystems = new UnOrderMultiMap<Type, IStartSystem>();

		//实际存储所有继承自DestroySystem的类,因为DestroySystem继承自IDestroySystem
		private readonly UnOrderMultiMap<Type, IDestroySystem> destroySystems = new UnOrderMultiMap<Type, IDestroySystem>();

		//实际存储所有继承自LoadSystem的类,因为LoadSystem继承自ILoadSystem
		private readonly UnOrderMultiMap<Type, ILoadSystem> loadSystems = new UnOrderMultiMap<Type, ILoadSystem>();

		//实际存储所有继承自UpdateSystem的类,因为UpdateSystem继承自IUpdateSystem
		private readonly UnOrderMultiMap<Type, IUpdateSystem> updateSystems = new UnOrderMultiMap<Type, IUpdateSystem>();

		//实际存储所有继承自LateUpdateSystem的类,因为LateUpdateSystem继承自ILateUpdateSystem
		private readonly UnOrderMultiMap<Type, ILateUpdateSystem> lateUpdateSystems = new UnOrderMultiMap<Type, ILateUpdateSystem>();

		//实际存储所有继承自ChangeSystem的类,因为ChangeSystem继承自IChangeSystem
		private readonly UnOrderMultiMap<Type, IChangeSystem> changeSystems = new UnOrderMultiMap<Type, IChangeSystem>();

		//实际存储所有继承自DeserializeSystem的类,因为DeserializeSystem继承自IDeserializeSystem
		private readonly UnOrderMultiMap<Type, IDeserializeSystem> deserializeSystems = new UnOrderMultiMap<Type, IDeserializeSystem>();

		private Queue<long> updates = new Queue<long>();
		private Queue<long> updates2 = new Queue<long>();
		
		private readonly Queue<long> starts = new Queue<long>();

		private Queue<long> loaders = new Queue<long>();
		private Queue<long> loaders2 = new Queue<long>();

		private Queue<long> lateUpdates = new Queue<long>();
		private Queue<long> lateUpdates2 = new Queue<long>();

		public void Add(DLLType dllType, Assembly assembly)
		{
			//缓存到字典中
			this.assemblies[dllType] = assembly;
			this.types.Clear();//清空

			//Log.Debug($"assemblies:{this.assemblies.Values.Count}");

			//扫描字典中的所有dll 实际上就只有Unity.Model.dll
			foreach (Assembly value in this.assemblies.Values)
			{
                //遍历每个程序集下的类型
				foreach (Type type in value.GetTypes())
				{
                    //获取定义的特性
					object[] objects = type.GetCustomAttributes(typeof(BaseAttribute), false);
					if (objects.Length == 0)
					{
						continue;
					}

					BaseAttribute baseAttribute = (BaseAttribute) objects[0];
					//根据特性进行缓存各种事件
					this.types.Add(baseAttribute.AttributeType, type);
					//Log.Debug($"{baseAttribute.AttributeType.ToString()}/ {type.ToString()}");
				}
			}

			//清空所有存储事件的字典
			this.awakeSystems.Clear();
			this.lateUpdateSystems.Clear();
			this.updateSystems.Clear();
			this.startSystems.Clear();
			this.loadSystems.Clear();
			this.changeSystems.Clear();
			this.destroySystems.Clear();
			this.deserializeSystems.Clear();

			//遍历ObjectSystemAttribute类型的事件
			foreach (Type type in types[typeof(ObjectSystemAttribute)])
			{
				//从这个类型 获取特性列表 类型是ObjectSystemAttribute 并且非继承的
				object[] attrs = type.GetCustomAttributes(typeof(ObjectSystemAttribute), false);

				if (attrs.Length == 0)
				{
					continue;
				}
                //创造该类型对应的实例
				object obj = Activator.CreateInstance(type);
				//进行类型的匹配 然后缓存到各自的字典中
				//以下这些都是接口interface,所以只要obj继承了以下某一个分支,那么也会满足以下该分支的条件判断
				//说白话就是将继承自IAwakeSystem、IUpdateSystem...并且加了ObjectSystemAttribute特性的这些类,都缓存到各自的字典里去
				switch (obj)
				{
					case IAwakeSystem objectSystem:
						this.awakeSystems.Add(objectSystem.Type(), objectSystem);
						break;
					case IUpdateSystem updateSystem:
						this.updateSystems.Add(updateSystem.Type(), updateSystem);
						break;
					case ILateUpdateSystem lateUpdateSystem:
						this.lateUpdateSystems.Add(lateUpdateSystem.Type(), lateUpdateSystem);
						break;
					case IStartSystem startSystem:
						this.startSystems.Add(startSystem.Type(), startSystem);
						break;
					case IDestroySystem destroySystem:
						this.destroySystems.Add(destroySystem.Type(), destroySystem);
						break;
					case ILoadSystem loadSystem:
						this.loadSystems.Add(loadSystem.Type(), loadSystem);
						Log.Debug($"{loadSystem.Type()}.....................");
						break;
					case IChangeSystem changeSystem:
						this.changeSystems.Add(changeSystem.Type(), changeSystem);
						break;
					case IDeserializeSystem deserializeSystem:
						this.deserializeSystems.Add(deserializeSystem.Type(), deserializeSystem);
						break;
				}
			}
			//allEvents是存储所有继承自IEvent的类 
			//先清除再缓存
			this.allEvents.Clear();
            //找到EventAttribute的class
            foreach (Type type in types[typeof(EventAttribute)])
			{
				object[] attrs = type.GetCustomAttributes(typeof(EventAttribute), false);

				foreach (object attr in attrs)
				{
					EventAttribute aEventAttribute = (EventAttribute)attr;
                    //创建class的实例
					object obj = Activator.CreateInstance(type);
                    //所有EventAttribute特性的类 都需要继承自IEvent
                    IEvent iEvent = obj as IEvent;
					if (iEvent == null)
					{
						Log.Error($"{obj.GetType().Name} 没有继承IEvent");
					}
					//注册事件,key是特性声明时候标注的字符串,value是class名称,带命名空间
					//RegisterEvent方法内部是将所有实例缓存到allEvents字典中
					this.RegisterEvent(aEventAttribute.Type, iEvent);
				}
			}

			//调用所有缓存的Load事件
			this.Load();
		}

        /// <summary>
        /// 注册事件,无非就是存储到字典管理起来,方便拿取.
        /// key是特性声明时候标注的字符串,value是class名称,带命名空间 
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="e"></param>
		public void RegisterEvent(string eventId, IEvent e)
		{
            Log.Error("eventId:" + eventId + ":" + e);
			if (!this.allEvents.ContainsKey(eventId))
			{
				this.allEvents.Add(eventId, new List<IEvent>());
			}
			this.allEvents[eventId].Add(e);
		}

        /// <summary>
        /// 通过枚举获取对应的dll
        /// </summary>
        /// <param name="dllType"></param>
        /// <returns></returns>
		public Assembly Get(DLLType dllType)
		{
			return this.assemblies[dllType];
		}
		
        /// <summary>
        /// 获取某个命名空间下的所有类
        /// </summary>
        /// <param name="systemAttributeType"></param>
        /// <returns></returns>
		public List<Type> GetTypes(Type systemAttributeType)
		{
			if (!this.types.ContainsKey(systemAttributeType))
			{
				return new List<Type>();
			}
			return this.types[systemAttributeType];
		}

        /// <summary>
        /// 添加组件
        /// </summary>
        /// <param name="component"></param>
		public void Add(Component component)
		{
            //缓存所有的组件
			this.allComponents.Add(component.InstanceId, component);
            //获取组件类型
			Type type = component.GetType();

			if (this.loadSystems.ContainsKey(type))
			{
				this.loaders.Enqueue(component.InstanceId);
			}

			if (this.updateSystems.ContainsKey(type))
			{
				this.updates.Enqueue(component.InstanceId);
			}

			if (this.startSystems.ContainsKey(type))
			{
				this.starts.Enqueue(component.InstanceId);
			}

			if (this.lateUpdateSystems.ContainsKey(type))
			{
				this.lateUpdates.Enqueue(component.InstanceId);
			}
		}

        /// <summary>
        /// 根据实例ID进行移除
        /// </summary>
        /// <param name="instanceId"></param>
		public void Remove(long instanceId)
		{
			this.allComponents.Remove(instanceId);
		}

        /// <summary>
        /// 根据实例ID获取组件
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
		public Component Get(long instanceId)
		{
			Component component = null;
			this.allComponents.TryGetValue(instanceId, out component);
			return component;
		}

        /// <summary>
        /// 组件反序列化
        /// </summary>
        /// <param name="component"></param>
        public void Deserialize(Component component)
		{
            //根据组件类型 从字典获取到反序列化的对象
			List<IDeserializeSystem> iDeserializeSystems = this.deserializeSystems[component.GetType()];
			if (iDeserializeSystems == null)
			{
				return;
			}

			foreach (IDeserializeSystem deserializeSystem in iDeserializeSystems)
			{
				if (deserializeSystem == null)
				{
					continue;
				}

				try
				{
                    //反序列化系统的调度
					deserializeSystem.Run(component);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

        /// <summary>
        /// 初始化唤醒 实际就是执行缓存好的Awake方法而已
        /// </summary>
        /// <param name="component"></param>
		public void Awake(Component component)
		{
			List<IAwakeSystem> iAwakeSystems = this.awakeSystems[component.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (IAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				
				IAwake iAwake = aAwakeSystem as IAwake;
				if (iAwake == null)
				{
					continue;
				}

				try
				{
					iAwake.Run(component);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

        /// <summary>
        /// 初始化唤醒 实际就是执行缓存好的Awake方法而已 带一个参数
        /// </summary>
        /// <typeparam name="P1"></typeparam>
        /// <param name="component"></param>
        /// <param name="p1"></param>
		public void Awake<P1>(Component component, P1 p1)
		{
			List<IAwakeSystem> iAwakeSystems = this.awakeSystems[component.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (IAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				
				IAwake<P1> iAwake = aAwakeSystem as IAwake<P1>;
				if (iAwake == null)
				{
					continue;
				}

				try
				{
					iAwake.Run(component, p1);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

		public void Awake<P1, P2>(Component component, P1 p1, P2 p2)
		{
			List<IAwakeSystem> iAwakeSystems = this.awakeSystems[component.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (IAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				
				IAwake<P1, P2> iAwake = aAwakeSystem as IAwake<P1, P2>;
				if (iAwake == null)
				{
					continue;
				}

				try
				{
					iAwake.Run(component, p1, p2);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

		public void Awake<P1, P2, P3>(Component component, P1 p1, P2 p2, P3 p3)
		{
			List<IAwakeSystem> iAwakeSystems = this.awakeSystems[component.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (IAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}

				IAwake<P1, P2, P3> iAwake = aAwakeSystem as IAwake<P1, P2, P3>;
				if (iAwake == null)
				{
					continue;
				}

				try
				{
					iAwake.Run(component, p1, p2, p3);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

        /// <summary>
        /// 执行组件的变化事件
        /// </summary>
        /// <param name="component"></param>
		public void Change(Component component)
		{
			List<IChangeSystem> iChangeSystems = this.changeSystems[component.GetType()];
			if (iChangeSystems == null)
			{
				return;
			}

			foreach (IChangeSystem iChangeSystem in iChangeSystems)
			{
				if (iChangeSystem == null)
				{
					continue;
				}

				try
				{
					iChangeSystem.Run(component);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

        /// <summary>
        /// 组件的加载事件,在Add之后执行
        /// </summary>
		public void Load()
		{
			while (this.loaders.Count > 0)
			{
                //先出列
				long instanceId = this.loaders.Dequeue();
				Component component;
                //然后根据实例ID拿到组件
				if (!this.allComponents.TryGetValue(instanceId, out component))
				{
					continue;
				}
				if (component.IsDisposed)
				{
					continue;
				}
				
				List<ILoadSystem> iLoadSystems = this.loadSystems[component.GetType()];
				if (iLoadSystems == null)
				{
					continue;
				}

                //然后入队到loaders2 下面又放回loaders1 下次就可以继续执行load事件
                this.loaders2.Enqueue(instanceId);

				foreach (ILoadSystem iLoadSystem in iLoadSystems)
				{
					try
					{
						iLoadSystem.Run(component);
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
			}
            //物体辅助类 交换接口  将loaders与loaders2进行交换
            ObjectHelper.Swap(ref this.loaders, ref this.loaders2);
		}

        /// <summary>
        /// 在Update之前调度,就是执行组件的Start方法而已
        /// </summary>
		private void Start()
		{
			while (this.starts.Count > 0)
			{
				long instanceId = this.starts.Dequeue();
				Component component;
				if (!this.allComponents.TryGetValue(instanceId, out component))
				{
					continue;
				}

				List<IStartSystem> iStartSystems = this.startSystems[component.GetType()];
				if (iStartSystems == null)
				{
					continue;
				}
				
				foreach (IStartSystem iStartSystem in iStartSystems)
				{
					try
					{
						iStartSystem.Run(component);
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
			}
		}

        /// <summary>
        /// 销毁事件 在组件被回收Dispose的时候调度
        /// </summary>
        /// <param name="component"></param>
		public void Destroy(Component component)
		{
			List<IDestroySystem> iDestroySystems = this.destroySystems[component.GetType()];
			if (iDestroySystems == null)
			{
				return;
			}

			foreach (IDestroySystem iDestroySystem in iDestroySystems)
			{
				if (iDestroySystem == null)
				{
					continue;
				}

				try
				{
					iDestroySystem.Run(component);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

        /// <summary>
        /// 组件的Update事件
        /// </summary>
		public void Update()
		{
			this.Start();
			while (this.updates.Count > 0)
			{
                //出列 得到实例ID
				long instanceId = this.updates.Dequeue();

                //通过实例ID找到组件
				Component component;
				if (!this.allComponents.TryGetValue(instanceId, out component))
				{
					continue;
				}
                //如果已释放 继续遍历下一个组件
				if (component.IsDisposed)
				{
					continue;
				}

                //从updateSystems中取出key对应的
                List<IUpdateSystem> iUpdateSystems = this.updateSystems[component.GetType()];
				if (iUpdateSystems == null)
				{
					continue;
				}

                //将ID压入另一个队列
				this.updates2.Enqueue(instanceId);
                //执行组件的Update方法
				foreach (IUpdateSystem iUpdateSystem in iUpdateSystems)
				{
					try
					{
						iUpdateSystem.Run(component);
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
			}
            //调换
			ObjectHelper.Swap(ref this.updates, ref this.updates2);
		}

		public void LateUpdate()
		{
			while (this.lateUpdates.Count > 0)
			{
				long instanceId = this.lateUpdates.Dequeue();
				Component component;
				if (!this.allComponents.TryGetValue(instanceId, out component))
				{
					continue;
				}
				if (component.IsDisposed)
				{
					continue;
				}

				List<ILateUpdateSystem> iLateUpdateSystems = this.lateUpdateSystems[component.GetType()];
				if (iLateUpdateSystems == null)
				{
					continue;
				}

				this.lateUpdates2.Enqueue(instanceId);

				foreach (ILateUpdateSystem iLateUpdateSystem in iLateUpdateSystems)
				{
					try
					{
						iLateUpdateSystem.Run(component);
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
			}

			ObjectHelper.Swap(ref this.lateUpdates, ref this.lateUpdates2);
		}

        /// <summary>
        /// 事件的调度,传入标记了的特性名称作为事件派发的key
        /// </summary>
        /// <param name="type"></param>
		public void Run(string type)
		{
			List<IEvent> iEvents;
			if (!this.allEvents.TryGetValue(type, out iEvents))
			{
				return;
			}
			foreach (IEvent iEvent in iEvents)
			{
				try
				{
                    //如果事件不等于空 就执行
					iEvent?.Handle();
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

        /// <summary>
        /// 带一个参数的调度
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="type"></param>
        /// <param name="a"></param>
		public void Run<A>(string type, A a)
		{
			List<IEvent> iEvents;
			if (!this.allEvents.TryGetValue(type, out iEvents))
			{
				return;
			}
			foreach (IEvent iEvent in iEvents)
			{
				try
				{
					iEvent?.Handle(a);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

        /// <summary>
        /// 带两个参数的调度
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="type"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
		public void Run<A, B>(string type, A a, B b)
		{
			List<IEvent> iEvents;
			if (!this.allEvents.TryGetValue(type, out iEvents))
			{
				return;
			}
			foreach (IEvent iEvent in iEvents)
			{
				try
				{
					iEvent?.Handle(a, b);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

        /// <summary>
        /// 带三个参数的调度
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="type"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
		public void Run<A, B, C>(string type, A a, B b, C c)
		{
			List<IEvent> iEvents;
			if (!this.allEvents.TryGetValue(type, out iEvents))
			{
				return;
			}
			foreach (IEvent iEvent in iEvents)
			{
				try
				{
					iEvent?.Handle(a, b, c);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}
	}
}