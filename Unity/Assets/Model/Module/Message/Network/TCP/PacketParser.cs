using System;
using System.IO;

namespace ETModel
{
	public enum ParserState
	{
		PacketSize,
		PacketBody
	}
	
	public static class Packet
	{
		public const int PacketSizeLength2 = 2;
		public const int PacketSizeLength4 = 4;
		public const int MinPacketSize = 2;
		public const int OpcodeIndex = 0;
		public const int MessageIndex = 2;
	}

	public class PacketParser
	{
		/// <summary>
		/// 这里是存储接收消息的循环队列
		/// </summary>
		private readonly CircularBuffer buffer;
		private int packetSize;
		private ParserState state;
		/// <summary>
		/// 内存流
		/// </summary>
		public MemoryStream memoryStream;
		private bool isOK;
		private readonly int packetSizeLength;

		public PacketParser(int packetSizeLength, CircularBuffer buffer, MemoryStream memoryStream)
		{
			this.packetSizeLength = packetSizeLength;
			this.buffer = buffer;
			this.memoryStream = memoryStream;
		}

		public bool Parse()
		{
			if (this.isOK)
			{
				return true;
			}

			bool finish = false;
			while (!finish)
			{
				switch (this.state)
				{
					case ParserState.PacketSize:
						if (this.buffer.Length < this.packetSizeLength)
						{
							finish = true;
						}
						else
						{
							//读取包体长度 写入到内存中memoryStream
							this.buffer.Read(this.memoryStream.GetBuffer(), 0, this.packetSizeLength);
							
							switch (this.packetSizeLength)
							{
								case Packet.PacketSizeLength4://表示包体大小的字段是4个字节的话

									//从内存取出实际的包体长度
									this.packetSize = BitConverter.ToInt32(this.memoryStream.GetBuffer(), 0);
									//非法捕捉
									if (this.packetSize > ushort.MaxValue * 16 || this.packetSize < Packet.MinPacketSize)
									{
										throw new Exception($"recv packet size error, 可能是外网探测端口: {this.packetSize}");
									}
									break;
								case Packet.PacketSizeLength2://表示包体大小的字段是2个字节的话

									//从内存取出实际的包体长度
									this.packetSize = BitConverter.ToUInt16(this.memoryStream.GetBuffer(), 0);
									//非法捕捉
									if (this.packetSize > ushort.MaxValue || this.packetSize < Packet.MinPacketSize)
									{
										throw new Exception($"recv packet size error:, 可能是外网探测端口: {this.packetSize}");
									}
									break;
								default:
									throw new Exception("packet size byte count must be 2 or 4!");
							}
							//状态切换到读取包体的状态
							this.state = ParserState.PacketBody;
						}
						break;
					case ParserState.PacketBody:
						//如果可读的长度 小于包体的长度 就返回了等待下次读取
						if (this.buffer.Length < this.packetSize)
						{
							finish = true;
						}
						else
						{
							//将当前流中的位置重新设置回0 长度等于要取的包体实际长度
							this.memoryStream.Seek(0, SeekOrigin.Begin);
							this.memoryStream.SetLength(this.packetSize);
							//获得一个字节数组
							byte[] bytes = this.memoryStream.GetBuffer();
							//从buffer写入到字节数组中 实际上也就是写入到内存中 写入的长度 是上一个状态获取到的
							this.buffer.Read(bytes, 0, this.packetSize);
							this.isOK = true;

							//将状态重新设置回读取包体大小的状态
							this.state = ParserState.PacketSize;
							finish = true;
						}
						break;
				}
			}
			return this.isOK;
		}

		public MemoryStream GetPacket()
		{
			this.isOK = false;
			return this.memoryStream;
		}
	}
}