using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ETModel;

namespace ETHotfix
{
	[ObjectSystem]
	public class SessionAwakeSystem : AwakeSystem<Session, ETModel.Session>
	{
		public override void Awake(Session self, ETModel.Session session)
		{
			self.session = session;
			SessionCallbackComponent sessionComponent = self.session.AddComponent<SessionCallbackComponent>();
			sessionComponent.MessageCallback = (s, opcode, memoryStream) => { self.Run(s, opcode, memoryStream); };
			sessionComponent.DisposeCallback = s => { self.Dispose(); };
		}
	}

	/// <summary>
	/// 用来收发热更层的消息
	/// </summary>
	public class Session: Entity
	{
		public ETModel.Session session;

		private static int RpcId { get; set; }
		private readonly Dictionary<int, Action<IResponse>> requestCallback = new Dictionary<int, Action<IResponse>>();

		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			base.Dispose();

			foreach (Action<IResponse> action in this.requestCallback.Values.ToArray())
			{
				action.Invoke(new ResponseMessage { Error = this.session.Error });
			}

			this.requestCallback.Clear();

			this.session.Dispose();
		}

		public void Run(ETModel.Session s, ushort opcode, MemoryStream memoryStream)
		{
			OpcodeTypeComponent opcodeTypeComponent = Game.Scene.GetComponent<OpcodeTypeComponent>();
			object instance = opcodeTypeComponent.GetInstance(opcode);
			object message = this.session.Network.MessagePacker.DeserializeFrom(instance, memoryStream);

			if (OpcodeHelper.IsNeedDebugLogMessage(opcode))
			{
				Log.Msg(message);
			}

			IResponse response = message as IResponse;
			if (response == null)
			{
				Game.Scene.GetComponent<MessageDispatcherComponent>().Handle(session, new MessageInfo(opcode, message));
				return;
			}
			
			Action<IResponse> action;
			if (!this.requestCallback.TryGetValue(response.RpcId, out action))
			{
				throw new Exception($"not found rpc, response message: {StringHelper.MessageToStr(response)}");
			}
			this.requestCallback.Remove(response.RpcId);

			action(response);
		}

		public void Send(IMessage message)
		{
			ushort opcode = Game.Scene.GetComponent<OpcodeTypeComponent>().GetOpcode(message.GetType());
			this.Send(opcode, message);
		}

		public void Send(ushort opcode, IMessage message)
		{
			if (OpcodeHelper.IsNeedDebugLogMessage(opcode))
			{
				Log.Msg(message);
			}
			session.Send(opcode, message);
		}

		public ETTask<IResponse> Call(IRequest request)
		{
			int rpcId = ++RpcId;
			var tcs = new ETTaskCompletionSource<IResponse>();
			//向字典压入响应请求的处理方法
			//方法要当做参数保存 肯定是使用委托
			//而以后要调用方法 实际就是执行委托
			this.requestCallback[rpcId] = (response) =>
			{
				try
				{
					if (ErrorCode.IsRpcNeedThrowException(response.Error))
					{
						throw new RpcException(response.Error, response.Message);
					}
					//返回await等待的地方 继续执行下半部分的代码
					//或者说 执行缓存了await下半部分的代码,并传递返回的网络数据反序列化后得到的proto对象
					tcs.SetResult(response);
				}
				catch (Exception e)
				{
					tcs.SetException(new Exception($"Rpc Error: {request.GetType().FullName}", e));
				}
			};

			request.RpcId = rpcId;
			this.Send(request);
			//返回task
			return tcs.Task;
		}

		public ETTask<IResponse> Call(IRequest request, CancellationToken cancellationToken)
		{
			int rpcId = ++RpcId;
			var tcs = new ETTaskCompletionSource<IResponse>();

			this.requestCallback[rpcId] = (response) =>
			{
				try
				{
					if (ErrorCode.IsRpcNeedThrowException(response.Error))
					{
						throw new RpcException(response.Error, response.Message);
					}

					tcs.SetResult(response);
				}
				catch (Exception e)
				{
					tcs.SetException(new Exception($"Rpc Error: {request.GetType().FullName}", e));
				}
			};

			cancellationToken.Register(() => { this.requestCallback.Remove(rpcId); });

			request.RpcId = rpcId;

			this.Send(request);
			return tcs.Task;
		}
	}
}
