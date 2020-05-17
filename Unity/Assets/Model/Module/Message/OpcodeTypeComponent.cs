using System;
using System.Collections.Generic;

namespace ETModel
{
	[ObjectSystem]
	public class OpcodeTypeComponentSystem : AwakeSystem<OpcodeTypeComponent>
	{
		public override void Awake(OpcodeTypeComponent self)
		{
			self.Load();
		}
	}
	
	[ObjectSystem]
	public class OpcodeTypeComponentLoadSystem : LoadSystem<OpcodeTypeComponent>
	{
		public override void Load(OpcodeTypeComponent self)
		{
			self.Load();
		}
	}

	public class OpcodeTypeComponent : Component
	{
		private readonly DoubleMap<ushort, Type> opcodeTypes = new DoubleMap<ushort, Type>();
		
		private readonly Dictionary<ushort, object> typeMessages = new Dictionary<ushort, object>();

        //加载
		public void Load()
		{
            //数据结构的初始化
			this.opcodeTypes.Clear();
			this.typeMessages.Clear();

            //找到所有MessageAttribute特性的class
            List<Type> types = Game.EventSystem.GetTypes(typeof(MessageAttribute));
			foreach (Type type in types)
			{
				//找到非继承的 加了MessageAttribute为特性的类
				object[] attrs = type.GetCustomAttributes(typeof(MessageAttribute), false);
				if (attrs.Length == 0)
				{
					continue;
				}
				
				MessageAttribute messageAttribute = attrs[0] as MessageAttribute;
				if (messageAttribute == null)
				{
					continue;
				}
                //加入到数据结构中
				this.opcodeTypes.Add(messageAttribute.Opcode, type);
                //加入的时候 并且通过它的类型创建实例
				this.typeMessages.Add(messageAttribute.Opcode, Activator.CreateInstance(type));
			}
		}

        //根据类型获取key
		public ushort GetOpcode(Type type)
		{
			return this.opcodeTypes.GetKeyByValue(type);
		}

        //根据操作码获取类型
		public Type GetType(ushort opcode)
		{
			return this.opcodeTypes.GetValueByKey(opcode);
		}
		
		// 客户端为了0GC需要消息池，服务端消息需要跨协程不需要消息池
		public object GetInstance(ushort opcode)
		{
#if SERVER
			Type type = this.GetType(opcode);
			if (type == null)
			{
				// 服务端因为有人探测端口，有可能会走到这一步，如果找不到opcode，抛异常
				throw new Exception($"not found opcode: {opcode}");
			}
			return Activator.CreateInstance(type);
#else
			return this.typeMessages[opcode];
#endif
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