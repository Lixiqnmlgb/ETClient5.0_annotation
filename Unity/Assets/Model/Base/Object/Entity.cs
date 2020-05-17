using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace ETModel
{
	[BsonIgnoreExtraElements]//忽略额外的元素 Todo
	public class Entity : ComponentWithId
	{
        //哈希表 存储组件
		[BsonElement("C")]
		[BsonIgnoreIfNull]
		private HashSet<Component> components = new HashSet<Component>();

        //字典 根据类型存储组件
		[BsonIgnore]
		private Dictionary<Type, Component> componentDict = new Dictionary<Type, Component>();

		public Entity()
		{
		}

		protected Entity(long id): base(id)
		{
		}

        //释放的接口
		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			base.Dispose();
			//从字典的值取出 然后调度释放
			foreach (Component component in this.componentDict.Values)
			{
				try
				{
					component.Dispose();
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
			//清空缓存
			this.components.Clear();
			this.componentDict.Clear();
		}
		
        //添加组件
		public virtual Component AddComponent(Component component)
		{
			Type type = component.GetType();
            //如果组件已存在 那么抛出异常
			if (this.componentDict.ContainsKey(type))
			{
				throw new Exception($"AddComponent, component already exist, id: {this.Id}, component: {type.Name}");
			}
			
            //组件的父物体
			component.Parent = this;
            //缓存到字典里
			this.componentDict.Add(type, component);
            //缓存到哈希表里 如果是可以序列化的组件,也就是同时继承了ISerializeToEntity接口
            if (component is ISerializeToEntity)
			{
				this.components.Add(component);
			}
			
			return component;
		}

        //根据类型进行添加组件 并且设置父物体为自身
		public virtual Component AddComponent(Type type)
		{
			if (this.componentDict.ContainsKey(type))
			{
				throw new Exception($"AddComponent, component already exist, id: {this.Id}, component: {type.Name}");
			}

			Component component = ComponentFactory.CreateWithParent(type, this, this.IsFromPool);

			this.componentDict.Add(type, component);
			
			if (component is ISerializeToEntity)
			{
				this.components.Add(component);
			}
			
			return component;
		}

        //根据泛型进行创建组件
		public virtual K AddComponent<K>() where K : Component, new()
		{
			Type type = typeof (K);
			if (this.componentDict.ContainsKey(type))
			{
				throw new Exception($"AddComponent, component already exist, id: {this.Id}, component: {typeof(K).Name}");
			}

			K component = ComponentFactory.CreateWithParent<K>(this, this.IsFromPool);

			this.componentDict.Add(type, component);
			
			if (component is ISerializeToEntity)
			{
				this.components.Add(component);
			}
			
			return component;
		}

        //给实体添加组件 并且带1个参数 在添加后传递给Awake方法
		public virtual K AddComponent<K, P1>(P1 p1) where K : Component, new()
		{
			Type type = typeof (K);
			if (this.componentDict.ContainsKey(type))
			{
				throw new Exception($"AddComponent, component already exist, id: {this.Id}, component: {typeof(K).Name}");
			}

			K component = ComponentFactory.CreateWithParent<K, P1>(this, p1, this.IsFromPool);
			
			this.componentDict.Add(type, component);
			
			if (component is ISerializeToEntity)
			{
				this.components.Add(component);
			}
			
			return component;
		}

        //给实体添加组件 并且带2个参数 在添加后传递给Awake方法
        public virtual K AddComponent<K, P1, P2>(P1 p1, P2 p2) where K : Component, new()
		{
			Type type = typeof (K);
			if (this.componentDict.ContainsKey(type))
			{
				throw new Exception($"AddComponent, component already exist, id: {this.Id}, component: {typeof(K).Name}");
			}

			K component = ComponentFactory.CreateWithParent<K, P1, P2>(this, p1, p2, this.IsFromPool);
			
			this.componentDict.Add(type, component);
			
			if (component is ISerializeToEntity)
			{
				this.components.Add(component);
			}
			
			return component;
		}

        //给实体添加组件 并且带3个参数 在添加后传递给Awake方法
        public virtual K AddComponent<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3) where K : Component, new()
		{
			Type type = typeof (K);
			if (this.componentDict.ContainsKey(type))
			{
				throw new Exception($"AddComponent, component already exist, id: {this.Id}, component: {typeof(K).Name}");
			}

			K component = ComponentFactory.CreateWithParent<K, P1, P2, P3>(this, p1, p2, p3, this.IsFromPool);
			
			this.componentDict.Add(type, component);
			
			if (component is ISerializeToEntity)
			{
				this.components.Add(component);
			}
			
			return component;
		}

        //根据泛型进行移除组件
		public virtual void RemoveComponent<K>() where K : Component
		{
			if (this.IsDisposed)
			{
				return;
			}
			Type type = typeof (K);
			Component component;
            //如果缓存的字典未添加 则无需任何操作
			if (!this.componentDict.TryGetValue(type, out component))
			{
				return;
			}
            //否则从缓存的数据结构中移除掉
			this.componentDict.Remove(type);
			this.components.Remove(component);
            //调用移除的组件的释放接口
			component.Dispose();
		}

        //根据类型进行移除组件
		public virtual void RemoveComponent(Type type)
		{
			if (this.IsDisposed)
			{
				return;
			}
			Component component;
			if (!this.componentDict.TryGetValue(type, out component))
			{
				return;
			}

			this.componentDict.Remove(type);
			this.components.Remove(component);

			component.Dispose();
		}

        //根据泛型获取组件
		public K GetComponent<K>() where K : Component
		{
			Component component;
			if (!this.componentDict.TryGetValue(typeof(K), out component))
			{
				return default(K);
			}
			return (K)component;
		}

        //根据类型获取组件
		public Component GetComponent(Type type)
		{
			Component component;
			if (!this.componentDict.TryGetValue(type, out component))
			{
				return null;
			}
			return component;
		}

        //获取物体上的所有组件
		public Component[] GetComponents()
		{
			return this.componentDict.Values.ToArray();
		}
		
        //结束初始化
		public override void EndInit()
		{
			try
			{
				base.EndInit();
				//字典先清空掉
				this.componentDict.Clear();

                //缓存操作 以及设置每个组件的父物体
				if (this.components != null)
				{
					foreach (Component component in this.components)
					{
						component.Parent = this;
						this.componentDict.Add(component.GetType(), component);
					}
				}
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}
	}
}