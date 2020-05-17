using System;
using System.Collections.Generic;
using System.IO;

namespace ETModel
{
	/// <summary>
	/// 循环缓冲器
	/// </summary>
	public class CircularBuffer: Stream
    {
        public int ChunkSize = 8192;

        private readonly Queue<byte[]> bufferQueue = new Queue<byte[]>();

        private readonly Queue<byte[]> bufferCache = new Queue<byte[]>();

        public int LastIndex { get; set; }

        public int FirstIndex { get; set; }
		
        private byte[] lastBuffer;

	    public CircularBuffer()
	    {
		    this.AddLast();
	    }

		/// <summary>
		/// 获取队列的长度
		/// </summary>
        public override long Length
        {
            get
            {
                int c = 0;
                if (this.bufferQueue.Count == 0)
                {
                    c = 0;
                }
                else
                {
					//(队列的数量-1)*8192 + 最近的索引 - 第一个索引
                    c = (this.bufferQueue.Count - 1) * ChunkSize + this.LastIndex - this.FirstIndex;
                }
                if (c < 0)
                {
                    Log.Error("CircularBuffer count < 0: {0}, {1}, {2}".Fmt(this.bufferQueue.Count, this.LastIndex, this.FirstIndex));
                }
                return c;
            }
        }

		/// <summary>
		/// 添加上一个
		/// </summary>
        public void AddLast()
        {
            byte[] buffer;
			//如果缓存队列大于0
            if (this.bufferCache.Count > 0)
            {
				//从缓存队列中出列
                buffer = this.bufferCache.Dequeue();
            }
			//如果缓存队列为0
            else
            {
				//构建一个新的 8192
                buffer = new byte[ChunkSize];
            }
			//压入主队列
            this.bufferQueue.Enqueue(buffer);
			//上一个队列等于以上逻辑取到的buffer
            this.lastBuffer = buffer;
        }

		/// <summary>
		/// 移除掉第一个
		/// </summary>
        public void RemoveFirst()
        {
			//从主队列bufferQueue取出第一个 加入到缓存队列
			this.bufferCache.Enqueue(bufferQueue.Dequeue());
        }

		/// <summary>
		/// 第一个元素 
		/// </summary>
        public byte[] First
        {
            get
            {
                if (this.bufferQueue.Count == 0)
                {
					//8092长度
					this.AddLast();
                }
                return this.bufferQueue.Peek();
            }
        }

		/// <summary>
		/// 获取上一个元素
		/// </summary>
        public byte[] Last
        {
            get
            {
                if (this.bufferQueue.Count == 0)
                {
                    this.AddLast();
                }
                return this.lastBuffer;
            }
        }

		/// <summary>
		/// 从CircularBuffer读到stream中
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public async ETTask ReadAsync(Stream stream)
	    {
		    long buffLength = this.Length;
			int sendSize = this.ChunkSize - this.FirstIndex;
		    if (sendSize > buffLength)
		    {
			    sendSize = (int)buffLength;
		    }
			
		    await stream.WriteAsync(this.First, this.FirstIndex, sendSize);
		    
		    this.FirstIndex += sendSize;
		    if (this.FirstIndex == this.ChunkSize)
		    {
			    this.FirstIndex = 0;
			    this.RemoveFirst();
		    }
		}

	    // 从CircularBuffer读到stream
	    public void Read(Stream stream, int count)
	    {
		    if (count > this.Length)
		    {
			    throw new Exception($"bufferList length < count, {Length} {count}");
		    }

		    int alreadyCopyCount = 0;
		    while (alreadyCopyCount < count)
		    {
			    int n = count - alreadyCopyCount;
			    if (ChunkSize - this.FirstIndex > n)
			    {
				    stream.Write(this.First, this.FirstIndex, n);
				    this.FirstIndex += n;
				    alreadyCopyCount += n;
			    }
			    else
			    {
				    stream.Write(this.First, this.FirstIndex, ChunkSize - this.FirstIndex);
				    alreadyCopyCount += ChunkSize - this.FirstIndex;
				    this.FirstIndex = 0;
				    this.RemoveFirst();
			    }
		    }
	    }
	    
	    // 从stream写入CircularBuffer
	    public void Write(Stream stream)
		{
			int count = (int)(stream.Length - stream.Position);
			
			int alreadyCopyCount = 0;
			while (alreadyCopyCount < count)
			{
				//如果上一次写入的位置 = 8192 说明已经将lastBuffer填充满了 
				if (this.LastIndex == ChunkSize)
				{
					this.AddLast();
					this.LastIndex = 0;
				}

				int n = count - alreadyCopyCount;
				//说明说明lastbuff能够存储完整的报文
				if (ChunkSize - this.LastIndex > n)
				{
					//读取流 放到lastBuffer去
					stream.Read(this.lastBuffer, this.LastIndex, n);
					//更新上一次读取的位置 等于上一次读取的位置+取出的数量
					this.LastIndex += count - alreadyCopyCount;
					//已复制的数量+=已读取的.
					alreadyCopyCount += n;
				}
				//说明lastbuff无法存储完整的报文了
				else
				{
					//那么能读取多少就读取多少 放入lastBuffer里去
					stream.Read(this.lastBuffer, this.LastIndex, ChunkSize - this.LastIndex);
					//更新已复制的数量 =写入的数量
					alreadyCopyCount += ChunkSize - this.LastIndex;
					//更新上一次写入的位置
					this.LastIndex = ChunkSize;
				}
			}
		}
	    

	    /// <summary>
		///  从stream写入CircularBuffer
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public async ETTask<int> WriteAsync(Stream stream)
	    {
		    int size = this.ChunkSize - this.LastIndex;
		    
		    int n = await stream.ReadAsync(this.Last, this.LastIndex, size);

		    if (n == 0)
		    {
			    return 0;
		    }

		    this.LastIndex += n;

		    if (this.LastIndex == this.ChunkSize)
		    {
			    this.AddLast();
			    this.LastIndex = 0;
		    }

		    return n;
	    }

	    // 把CircularBuffer中数据写入buffer
        public override int Read(byte[] buffer, int offset, int count)
        {
	        if (buffer.Length < offset + count)
	        {
		        throw new Exception($"bufferList length < coutn, buffer length: {buffer.Length} {offset} {count}");
	        }

	        long length = this.Length;
			if (length < count)
            {
	            count = (int)length;
            }

			//已复制的数量
            int alreadyCopyCount = 0;
            while (alreadyCopyCount < count)
            {
				//要写入的数量
                int n = count - alreadyCopyCount;
				//buffer容量充足 可以写入
				if (ChunkSize - this.FirstIndex > n)
                {
					//获取队列的第一个元素 复制到buffer中
                    Array.Copy(this.First, this.FirstIndex, buffer, alreadyCopyCount + offset, n);
					//更新读取的起始索引
                    this.FirstIndex += n;
					//更新已复制的数量
                    alreadyCopyCount += n;
                }
				//buffer容量不充足
				else
				{
					//那就能读取多少读取多少
                    Array.Copy(this.First, this.FirstIndex, buffer, alreadyCopyCount + offset, ChunkSize - this.FirstIndex);
                    alreadyCopyCount += ChunkSize - this.FirstIndex;
                    this.FirstIndex = 0;
					//移除掉第一个,因为已经全读取完了
					//实际是放到缓存队列中去了 进行重复利用 
					//在写入数据的时候 如果需要数组来缓存 就可以从缓存的队列获取了 而非再一次new
                    this.RemoveFirst();
                }
            }

	        return count;
        }

		/// <summary>
		/// lastBuffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		public override void Write(byte[] buffer, int offset, int count)
        {
	        int alreadyCopyCount = 0;
			//已复制的数量 < 要写入的数量
            while (alreadyCopyCount < count)
            {
				//最近一次索引==8192 说明lastBuffer已经写满了
				if (this.LastIndex == ChunkSize)
                {
					//更新lastBuffer
					this.AddLast();
					//上一次的索引设置为0
                    this.LastIndex = 0;
                }

				//n是剩下要写入的数量 =写入的数量-已复制的数量
				int n = count - alreadyCopyCount;
				//8192-上一次写入的位置 还大于要写入的数据 就说明lastBuffer容量充足,可以写入数据
				if (ChunkSize - this.LastIndex > n)
                {
                    Array.Copy(buffer, alreadyCopyCount + offset, this.lastBuffer, this.LastIndex, n);
					//
                    this.LastIndex += count - alreadyCopyCount;
                    alreadyCopyCount += n;
                }
				//如果不可以写入数据了
                else
                {
					//能复制多少就复制多少
					//下一次循环会重新更新lastBuffere
                    Array.Copy(buffer, alreadyCopyCount + offset, this.lastBuffer, this.LastIndex, ChunkSize - this.LastIndex);
                    alreadyCopyCount += ChunkSize - this.LastIndex;
                    this.LastIndex = ChunkSize;
                }
            }
        }

	    public override void Flush()
	    {
		    throw new NotImplementedException();
		}

	    public override long Seek(long offset, SeekOrigin origin)
	    {
			throw new NotImplementedException();
	    }

	    public override void SetLength(long value)
	    {
		    throw new NotImplementedException();
		}

		
	    public override bool CanRead
	    {
		    get
		    {
			    return true;
		    }
	    }

	    public override bool CanSeek
	    {
		    get
		    {
			    return false;
		    }
	    }

	    public override bool CanWrite
	    {
		    get
		    {
			    return true;
		    }
	    }

	    public override long Position { get; set; }
    }
}