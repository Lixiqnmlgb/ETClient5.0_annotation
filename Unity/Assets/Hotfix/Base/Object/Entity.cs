using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace ETHotfix
{
	[BsonIgnoreExtraElements]
	public class Entity : ComponentWithId
	{
		[BsonElement("C")]
		[BsonIgnoreIfNull]
		private HashSet<Component> components = new HashSet<Component>();

		[BsonIgnore]
		private Dictionary<Type, Component> componentDict = new Dictionary<Type, Component>();

		public Entity()
		{
		}

		protected Entity(long id): base(id)
		{
		}

		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			base.Dispose();
			
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
			
			this.components.Clear();
			this.componentDict.Clear();
		}
		
		public virtual Component AddComponent(Component component)
		{
			Type type = component.GetType();
			if (this.componentDict.ContainsKey(type))
			{
				throw new Exception($"AddComponent, component already exist, id: {this.Id}, component: {type.Name}");
			}
			
			component.Parent = this;

			this.componentDict.Add(type, component);

			if (component is ISerializeToEntity)
			{
				this.components.Add(component);
			}
			
			return component;
		}

		public virtual Component AddComponent(Type type)
		{
			if (this.componentDict.ContainsKey(type))
			{
				throw new Exception($"AddComponent, component already exist, id: {this.Id}, component: {type.Name}");
			}

			//创建组件 并且设置父物体为该实体 
			//CreateWithParent 内部就调用了Awake方法
			Component component = ComponentFactory.CreateWithParent(type, this, this.IsFromPool);

			//缓存到字典中
			this.componentDict.Add(type, component);
			
			//如果是可以序列化的 缓存到哈希表中
			if (component is ISerializeToEntity)
			{
				this.components.Add(component);
			}
			
			return component;
		}

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

		public virtual void RemoveComponent<K>() where K : Component
		{
			if (this.IsDisposed)
			{
				return;
			}
			Type type = typeof (K);
			Component component;
			//如果找不到组件 就跳出返回
			if (!this.componentDict.TryGetValue(type, out component))
			{
				return;
			}
			//从缓存的数据结构移除掉
			this.componentDict.Remove(type);
			this.components.Remove(component);
			//调用组件的释放接口
			component.Dispose();
		}

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

		public K GetComponent<K>() where K : Component
		{
			Component component;
			//如果不存在xx类型
			if (!this.componentDict.TryGetValue(typeof(K), out component))
			{
				//返回K的默认值 一般为null
				return default(K);
			}
			return (K)component;
		}

		public Component GetComponent(Type type)
		{
			Component component;
			if (!this.componentDict.TryGetValue(type, out component))
			{
				return null;
			}
			return component;
		}

		public Component[] GetComponents()
		{
			return this.componentDict.Values.ToArray();
		}
		
		public override void EndInit()
		{
			try
			{
				base.EndInit();
				
				this.componentDict.Clear();

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