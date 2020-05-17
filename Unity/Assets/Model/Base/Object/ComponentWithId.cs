using MongoDB.Bson.Serialization.Attributes;
#if !SERVER
using UnityEngine;
#endif

namespace ETModel
{
    //带ID的组件
	[BsonIgnoreExtraElements]
	public abstract class ComponentWithId : Component
	{
		[BsonIgnoreIfDefault]
		[BsonDefaultValue(0L)]
		[BsonElement]
		[BsonId]
		public long Id { get; set; }

        //自动生成
		protected ComponentWithId()
		{
			this.Id = this.InstanceId;
		}

        //构造函数 子类调用的时候传递Id
		protected ComponentWithId(long id)
		{
			this.Id = id;
		}

        //释放
		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}

			base.Dispose();
		}
	}
}