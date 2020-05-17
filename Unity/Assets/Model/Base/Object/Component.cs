using System;
using ETModel;
using MongoDB.Bson.Serialization.Attributes;
#if !SERVER
using UnityEngine;
#endif

namespace ETModel
{
	[BsonIgnoreExtraElements]
	public abstract class Component : Object, IDisposable
	{
		[BsonIgnore]
		public long InstanceId { get; private set; }
		
#if !SERVER
		public static GameObject Global { get; } = GameObject.Find("/Global");
		
		[BsonIgnore]
		public GameObject GameObject { get; protected set; }
#endif

		[BsonIgnore]
		private bool isFromPool;

        //是否来自对象池
        [BsonIgnore]
		public bool IsFromPool
		{
			get
			{
				return this.isFromPool;
			}
			set
			{
				this.isFromPool = value;

				if (!this.isFromPool)
				{
					return;
				}

				if (this.InstanceId == 0)
				{
                    //生成一个ID
					this.InstanceId = IdGenerater.GenerateInstanceId();
				}
			}
		}

		[BsonIgnore]
        public bool IsDisposed
        {//是否已释放
            get
			{
				return this.InstanceId == 0;
			}
		}
        
		private Component parent;
		
		[BsonIgnore]
		public Component Parent
        {//父物体
            get
			{
				return this.parent;
			}
			set
			{
				this.parent = value;

#if !SERVER
				if (this.parent == null)
				{
					this.GameObject.transform.SetParent(Global.transform, false);
					return;
				}

				if (this.GameObject != null && this.parent.GameObject != null)
				{
					this.GameObject.transform.SetParent(this.parent.GameObject.transform, false);
				}
#endif
			}
		}

        //获取父物体
		public T GetParent<T>() where T : Component
		{
			return this.Parent as T;
		}

        //父物体的实体对象
		[BsonIgnore]
		public Entity Entity
		{
			get
			{
				return this.Parent as Entity;
			}
		}
		
        //构造函数
		protected Component()
		{
			this.InstanceId = IdGenerater.GenerateInstanceId();
#if !SERVER
            //如果不是标记了隐藏的特性,那么不在Hierarchy面板中隐藏,并且要求是继承的对象
            if (!this.GetType().IsDefined(typeof(HideInHierarchy), true))
			{
				this.GameObject = new GameObject();
				this.GameObject.name = this.GetType().Name;
                //层级设置为隐藏的"Hidden"
                this.GameObject.layer = LayerNames.GetLayerInt(LayerNames.HIDDEN);
				this.GameObject.transform.SetParent(Global.transform, false);
                //组件视图?
				this.GameObject.AddComponent<ComponentView>().Component = this;
			}
#endif
		}


		public virtual void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			// 触发Destroy事件
			Game.EventSystem.Destroy(this);
            //从缓存中移除掉
			Game.EventSystem.Remove(this.InstanceId);
			
			this.InstanceId = 0;

			if (this.IsFromPool)
			{
                //对象池进行回收
				Game.ObjectPool.Recycle(this);
			}
			else
			{
#if !SERVER
                //如果不是对象创建的 那么释放的时候 直接进行销毁
				if (this.GameObject != null)
				{
					UnityEngine.Object.Destroy(this.GameObject);
				}
#endif
			}
		}

        //初始化结束
        public override void EndInit()
		{
			Game.EventSystem.Deserialize(this);
		}
		
		public override string ToString()
		{
            //打印出来
			return MongoHelper.ToJson(this);
		}
	}
}