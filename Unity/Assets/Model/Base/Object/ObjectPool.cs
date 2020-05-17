using System;
using System.Collections.Generic;

namespace ETModel
{
    /// <summary>
    /// 组件队列
    /// </summary>
    public class ComponentQueue: Component
	{
		public string TypeName { get; }
		
        //组件的队列
		private readonly Queue<Component> queue = new Queue<Component>();

		public ComponentQueue(string typeName)
		{
			this.TypeName = typeName;
		}

        //入队
		public void Enqueue(Component component)
		{
			component.Parent = this;
			this.queue.Enqueue(component);
		}

        //出队
		public Component Dequeue()
		{
			return this.queue.Dequeue();
		}

        //返回队列开头的元素 但不进行移除
		public Component Peek()
		{
			return this.queue.Peek();
		}

        //获取数量
		public int Count
		{
			get
			{
				return this.queue.Count;
			}
		}

        //释放
		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			base.Dispose();

			while (this.queue.Count > 0)
			{
				Component component = this.queue.Dequeue();
				component.IsFromPool = false;
				component.Dispose();
			}
		}
	}
	
    /// <summary>
    /// 物体对象池
    /// </summary>
    public class ObjectPool: Component
    {
	    public string Name { get; set; }

        //根据类型缓存组件队列
        private readonly Dictionary<Type, ComponentQueue> dictionary = new Dictionary<Type, ComponentQueue>();

        //通过类型获取
        public Component Fetch(Type type)
        {
	        Component obj;
            //如果缓存中不包含这个类型的 就创建类型对应的实例
            if (!this.dictionary.TryGetValue(type, out ComponentQueue queue))
            {
	            obj = (Component)Activator.CreateInstance(type);
            }
            //或者队列的数量是等于0的
	        else if (queue.Count == 0)
            {
	            obj = (Component)Activator.CreateInstance(type);
            }
            //否则出列获取
            else
            {
	            obj = queue.Dequeue();
            }
	        
	        obj.IsFromPool = true;
            return obj;
        }

        //泛型获取
        public T Fetch<T>() where T: Component
		{
            T t = (T) this.Fetch(typeof(T));
			return t;
		}
        
        //回收
        public void Recycle(Component obj)
        {
	        obj.Parent = this;
            Type type = obj.GetType();
	        ComponentQueue queue;
            if (!this.dictionary.TryGetValue(type, out queue))
            {
                queue = new ComponentQueue(type.Name);
	            queue.Parent = this;
#if !SERVER
	            queue.GameObject.name = $"{type.Name}s";
#endif
                //压入回收的字典
				this.dictionary.Add(type, queue);
            }
            queue.Enqueue(obj);
        }

        //清理对象池
	    public void Clear()
	    {
		    foreach (var kv in this.dictionary)
		    {
			    kv.Value.IsFromPool = false;
			    kv.Value.Dispose();
		    }
		    this.dictionary.Clear();
	    }
    }
}