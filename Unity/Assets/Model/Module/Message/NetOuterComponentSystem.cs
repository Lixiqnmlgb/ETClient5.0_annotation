using ETModel;

namespace ETHotfix
{
	[ObjectSystem]
	public class NetOuterComponentAwakeSystem : AwakeSystem<NetOuterComponent>
	{
		public override void Awake(NetOuterComponent self)
		{
			self.Awake(self.Protocol);
			self.MessagePacker = new ProtobufPacker();
			self.MessageDispatcher = new OuterMessageDispatcher();
		}
	}

	[ObjectSystem]
	public class NetOuterComponentAwake1System : AwakeSystem<NetOuterComponent, string>
	{
		public override void Awake(NetOuterComponent self, string address)
		{
			self.Awake(self.Protocol, address);
			self.MessagePacker = new ProtobufPacker();
			self.MessageDispatcher = new OuterMessageDispatcher();
		}
	}
	
	[ObjectSystem]
	public class NetOuterComponentLoadSystem : LoadSystem<NetOuterComponent>
	{
		//事件系统初始化完就会调用到这个方法 
		public override void Load(NetOuterComponent self)
		{
			//构造ProtobufPacker,用于处理Proto的序列化和反序列化
			self.MessagePacker = new ProtobufPacker();
			//构造消息分发器
			self.MessageDispatcher = new OuterMessageDispatcher();
		}
	}
	
	[ObjectSystem]
	public class NetOuterComponentUpdateSystem : UpdateSystem<NetOuterComponent>
	{
		public override void Update(NetOuterComponent self)
		{
			self.Update();
		}
	}
}