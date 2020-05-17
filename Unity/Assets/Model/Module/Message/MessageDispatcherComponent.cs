using System;
using System.Collections.Generic;

namespace ETModel
{
	[ObjectSystem]
	public class MessageDispatcherComponentAwakeSystem : AwakeSystem<MessageDispatcherComponent>
	{
		public override void Awake(MessageDispatcherComponent t)
		{
			t.Awake();
		}
	}

	[ObjectSystem]
	public class MessageDispatcherComponentLoadSystem : LoadSystem<MessageDispatcherComponent>
	{
		public override void Load(MessageDispatcherComponent self)
		{
			self.Load();
		}
	}

	/// <summary>
	/// 消息分发组件
	/// </summary>
	public class MessageDispatcherComponent : Component
	{
		private readonly Dictionary<ushort, List<IMHandler>> handlers = new Dictionary<ushort, List<IMHandler>>();

		public void Awake()
		{
			this.Load();
		}
        //加载
		public void Load()
		{
			this.handlers.Clear();
			//先获取开发时候写的 所有用于处理网络消息的类 
			//加了MessageHandlerAttribute特性的
			List<Type> types = Game.EventSystem.GetTypes(typeof(MessageHandlerAttribute));

			foreach (Type type in types)
			{
                //找到非继承的
				object[] attrs = type.GetCustomAttributes(typeof(MessageHandlerAttribute), false);
				if (attrs.Length == 0)
				{
					continue;
				}
				//并且是直接或间接继承自IMHandler接口的 
				//其中AMHanlde就是继承自IMHandle,所以继承自AMHandler的类也可以转化为IMHandler
				//那么iMHandler就不会等于null了.
				IMHandler iMHandler = Activator.CreateInstance(type) as IMHandler;
				if (iMHandler == null)
				{
					Log.Error($"message handle {type.Name} 需要继承 IMHandler");
					continue;
				}
                //获取消息类型
				Type messageType = iMHandler.GetMessageType();
                //根据类型获取到操作码/协议号 opcode
				ushort opcode = this.Entity.GetComponent<OpcodeTypeComponent>().GetOpcode(messageType);
				if (opcode == 0)
				{
					Log.Error($"消息opcode为0: {messageType.Name}");
					continue;
				}
				//注册存储处理的方法,key:操作码 value:实例
				//后面调用Handle的时候就可以通过key找到实例,然后进行调用
				this.RegisterHandler(opcode, iMHandler);
			}
		}

        //注册 无非就是压入字典而已
		public void RegisterHandler(ushort opcode, IMHandler handler)
		{
            //因为同一个操作码 可能被多方监听 所以value是以list数据结构进行设计
            //如果字典不包含操作码  就直接new list,然后都要往这个list里压入
			if (!this.handlers.ContainsKey(opcode))
			{
				this.handlers.Add(opcode, new List<IMHandler>());
			}
			this.handlers[opcode].Add(handler);
		}


        //处理接口 从字典取出缓存 然后调用 调用时候进行传参MessageInfo
        public void Handle(Session session, MessageInfo messageInfo)
		{
			//取出存储的实例 
			List<IMHandler> actions;
			if (!this.handlers.TryGetValue(messageInfo.Opcode, out actions))
			{
				Log.Error($"消息没有处理: {messageInfo.Opcode} {JsonHelper.ToJson(messageInfo.Message)}");
				return;
			}
			
			foreach (IMHandler ev in actions)
			{
				try
				{
					//调用实例的Handle方法
					ev.Handle(session, messageInfo.Message);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

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