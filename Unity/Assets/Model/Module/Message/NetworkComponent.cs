using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ETModel
{
	public abstract class NetworkComponent : Component
	{
		public AppType AppType;
		
		protected AService Service;

		private readonly Dictionary<long, Session> sessions = new Dictionary<long, Session>();
		//用于序列化和反序列化网络报文的对象
		public IMessagePacker MessagePacker { get; set; }

		public IMessageDispatcher MessageDispatcher { get; set; }

        /// <summary>
		/// 根据协议类型进行初始化
		/// </summary>
		/// <param name="protocol">协议类型</param>
		/// <param name="packetSize">包体大小的占位 默认为2</param>
        public void Awake(NetworkProtocol protocol, int packetSize = Packet.PacketSizeLength2)
		{
			switch (protocol)
			{
				case NetworkProtocol.KCP:
					this.Service = new KService() { Parent = this };
					break;
				case NetworkProtocol.TCP:
					this.Service = new TService(packetSize) { Parent = this };
					break;
				case NetworkProtocol.WebSocket:
					this.Service = new WService() { Parent = this };
					break;
			}
		}

		
		public void Awake(NetworkProtocol protocol, string address, int packetSize = Packet.PacketSizeLength2)
		{
			try
			{
				IPEndPoint ipEndPoint;
				switch (protocol)
				{
					case NetworkProtocol.KCP:
						ipEndPoint = NetworkHelper.ToIPEndPoint(address);
						this.Service = new KService(ipEndPoint, this.OnAccept) { Parent = this };
						break;
					case NetworkProtocol.TCP:
						ipEndPoint = NetworkHelper.ToIPEndPoint(address);
						this.Service = new TService(packetSize, ipEndPoint, this.OnAccept) { Parent = this };
						break;
					case NetworkProtocol.WebSocket:
						string[] prefixs = address.Split(';');
						this.Service = new WService(prefixs, this.OnAccept) { Parent = this };
						break;
				}
			}
			catch (Exception e)
			{
				throw new Exception($"NetworkComponent Awake Error {address}", e);
			}
		}

		public int Count
		{
			get { return this.sessions.Count; }
		}

		public void OnAccept(AChannel channel)
		{
			Session session = ComponentFactory.CreateWithParent<Session, AChannel>(this, channel);
			this.sessions.Add(session.Id, session);
			session.Start();
		}

		public virtual void Remove(long id)
		{
			Session session;
			if (!this.sessions.TryGetValue(id, out session))
			{
				return;
			}
			this.sessions.Remove(id);
			session.Dispose();
		}

		public Session Get(long id)
		{
			Session session;
			this.sessions.TryGetValue(id, out session);
			return session;
		}

		/// <summary>
		/// 创建一个新Session
		/// </summary>
		public Session Create(IPEndPoint ipEndPoint)
		{
			AChannel channel = this.Service.ConnectChannel(ipEndPoint);
			Session session = ComponentFactory.CreateWithParent<Session, AChannel>(this, channel);
			this.sessions.Add(session.Id, session);
			session.Start();
			return session;
		}
		
		/// <summary>
		/// 创建一个Session
		/// </summary>
		public Session Create(string address)
		{
			//这里以TCP为例注释:
			//TCP的话 是调用TService的ConnectChannel 内部进行构建一个TChannel
			//注意:TChannel是对Socket的第一层封装,包括:连接、接收、发送、构建报文、拆解报文...为外部提供Socket交互的接口
			AChannel channel = this.Service.ConnectChannel(address);
			//创建session会话实体 将自身作为session的父物体 并且把TChannel传递进去
			Session session = ComponentFactory.CreateWithParent<Session, AChannel>(this, channel);
			//注意:框架会调用到session的Awake方法,并且将TChannel传递给Awake方法,然后才继续往下执行
			//管理该会话实体
			this.sessions.Add(session.Id, session);
			//调用会话实体的Start方法
			session.Start();
			return session;
		}

		public void Update()
		{
			if (this.Service == null)
			{
				return;
			}
			this.Service.Update();
		}

		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}

			base.Dispose();

			foreach (Session session in this.sessions.Values.ToArray())
			{
				session.Dispose();
			}

			this.Service.Dispose();
		}
	}
}