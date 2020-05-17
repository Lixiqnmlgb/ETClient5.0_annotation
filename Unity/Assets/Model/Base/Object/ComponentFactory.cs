using System;

namespace ETModel
{
	public static class ComponentFactory
	{
        //创建组件 并且设置父物体
		public static Component CreateWithParent(Type type, Component parent, bool fromPool = true)
		{
			Component component;
            //如果从对象池创建
			if (fromPool)
			{
				component = Game.ObjectPool.Fetch(type);
			}
			else
			{
                //否则直接通过Activator创建类型对应的实例
                component = (Component)Activator.CreateInstance(type);	
			}
			
            //事件系统对组件进行缓存
			Game.EventSystem.Add(component);
            //设置父物体
			component.Parent = parent;
            //类型模式的判断
			if (component is ComponentWithId componentWithId)
			{
				componentWithId.Id = component.InstanceId;
			}
            //调用组件的初始化函数
			Game.EventSystem.Awake(component);
			return component;
		}

        //通过泛型进行创建组件
		public static T CreateWithParent<T>(Component parent, bool fromPool = true) where T : Component
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);
			
			component.Parent = parent;
			if (component is ComponentWithId componentWithId)
			{
				componentWithId.Id = component.InstanceId;
			}
			Game.EventSystem.Awake(component);
			return component;
		}

        //通过泛型进行创建组件 并且携带了1个参数 用于传递给Awake方法
		public static T CreateWithParent<T, A>(Component parent, A a, bool fromPool = true) where T : Component
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);
			
			component.Parent = parent;
			if (component is ComponentWithId componentWithId)
			{
				componentWithId.Id = component.InstanceId;
			}
			Game.EventSystem.Awake(component, a);
			return component;
		}

        //通过泛型进行创建组件 并且携带了2个参数 用于传递给Awake函数方法
        public static T CreateWithParent<T, A, B>(Component parent, A a, B b, bool fromPool = true) where T : Component
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);
			
			component.Parent = parent;
			if (component is ComponentWithId componentWithId)
			{
				componentWithId.Id = component.InstanceId;
			}
			Game.EventSystem.Awake(component, a, b);
			return component;
		}

        //通过泛型进行创建组件 并且携带了3个参数 用于传递给Awake函数方法
        public static T CreateWithParent<T, A, B, C>(Component parent, A a, B b, C c, bool fromPool = true) where T : Component
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);
			
			component.Parent = parent;
			if (component is ComponentWithId componentWithId)
			{
				componentWithId.Id = component.InstanceId;
			}
			Game.EventSystem.Awake(component, a, b, c);
			return component;
		}

        //通过泛型进行创建组件 并且不设置父物体
        public static T Create<T>(bool fromPool = true) where T : Component
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);

			if (component is ComponentWithId componentWithId)
			{
				componentWithId.Id = component.InstanceId;
			}
			Game.EventSystem.Awake(component);
			return component;
		}
        //通过泛型进行创建组件 并且不设置父物体 携带1个参数 传递给Awake方法
        public static T Create<T, A>(A a, bool fromPool = true) where T : Component
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);

			if (component is ComponentWithId componentWithId)
			{
				componentWithId.Id = component.InstanceId;
			}
			Game.EventSystem.Awake(component, a);
			return component;
		}
        //通过泛型进行创建组件 并且不设置父物体 携带2个参数 传递给Awake方法
        public static T Create<T, A, B>(A a, B b, bool fromPool = true) where T : Component
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);

			if (component is ComponentWithId componentWithId)
			{
				componentWithId.Id = component.InstanceId;
			}
			Game.EventSystem.Awake(component, a, b);
			return component;
		}
        //通过泛型进行创建组件 并且不设置父物体 携带3个参数 传递给Awake方法
        public static T Create<T, A, B, C>(A a, B b, C c, bool fromPool = true) where T : Component
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);

			if (component is ComponentWithId componentWithId)
			{
				componentWithId.Id = component.InstanceId;
			}
			Game.EventSystem.Awake(component, a, b, c);
			return component;
		}
        //通过泛型进行创建组件 并且不设置父物体 携带ID 对组件ID进行自定义 而非自动生成
        public static T CreateWithId<T>(long id, bool fromPool = true) where T : ComponentWithId
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);
			
			component.Id = id;
			Game.EventSystem.Awake(component);
			return component;
		}

        //通过泛型进行创建组件 并且不设置父物体 携带ID 对组件ID进行自定义 而非自动生成 携带1个参数 传递给Awake方法
        public static T CreateWithId<T, A>(long id, A a, bool fromPool = true) where T : ComponentWithId
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);
			
			component.Id = id;
			Game.EventSystem.Awake(component, a);
			return component;
		}

        //通过泛型进行创建组件 并且不设置父物体 携带ID 对组件ID进行自定义 而非自动生成 携带2个参数 传递给Awake方法
        public static T CreateWithId<T, A, B>(long id, A a, B b, bool fromPool = true) where T : ComponentWithId
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);
			
			component.Id = id;
			Game.EventSystem.Awake(component, a, b);
			return component;
		}

        //通过泛型进行创建组件 并且不设置父物体 携带ID 对组件ID进行自定义 而非自动生成 携带3个参数 传递给Awake方法
        public static T CreateWithId<T, A, B, C>(long id, A a, B b, C c, bool fromPool = true) where T : ComponentWithId
		{
			Type type = typeof (T);
			
			T component;
			if (fromPool)
			{
				component = (T)Game.ObjectPool.Fetch(type);
			}
			else
			{
				component = (T)Activator.CreateInstance(type);	
			}
			
			Game.EventSystem.Add(component);
			
			component.Id = id;
			Game.EventSystem.Awake(component, a, b, c);
			return component;
		}
	}
}
