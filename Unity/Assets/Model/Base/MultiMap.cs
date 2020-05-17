using System.Collections.Generic;
using System.Linq;

namespace ETModel
{
    /// <summary>
    /// 自定义的数据结构,内部由SortedDictionary实现
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="K"></typeparam>
	public class MultiMap<T, K>
	{
		private readonly SortedDictionary<T, List<K>> dictionary = new SortedDictionary<T, List<K>>();

		// 重用list
		private readonly Queue<List<K>> queue = new Queue<List<K>>();

		public SortedDictionary<T, List<K>> GetDictionary()
		{
			return this.dictionary;
		}

		public void Add(T t, K k)
		{
			List<K> list;
			this.dictionary.TryGetValue(t, out list);
			if (list == null)
			{
                //获取列表
				list = this.FetchList();
				this.dictionary[t] = list;
			}
			list.Add(k);
		}

        //获取第一个
		public KeyValuePair<T, List<K>> First()
		{
			return this.dictionary.First();
		}

        //获取第一个键
		public T FirstKey()
		{
			return this.dictionary.Keys.First();
		}

        //获取数量
		public int Count
		{
			get
			{
				return this.dictionary.Count;
			}
		}

        //获取列表
		private List<K> FetchList()
		{
            //如果数量大于0 就出列 清除后进行返回
			if (this.queue.Count > 0)
			{
				List<K> list = this.queue.Dequeue();
				list.Clear();
				return list;
			}
			return new List<K>();
		}

        //回收列表
		private void RecycleList(List<K> list)
		{
			// 防止暴涨
			if (this.queue.Count > 100)
			{
				return;
			}
            //清除后 重新压入
			list.Clear();
			this.queue.Enqueue(list);
		}

        //移除
		public bool Remove(T t, K k)
		{
			List<K> list;
			this.dictionary.TryGetValue(t, out list);
			if (list == null)
			{
				return false;
			}
			if (!list.Remove(k))
			{
				return false;
			}
			if (list.Count == 0)
			{
				this.RecycleList(list);//重新压入空的到queue
                this.dictionary.Remove(t);
			}
			return true;
		}

        //移除
		public bool Remove(T t)
		{
			List<K> list = null;
			this.dictionary.TryGetValue(t, out list);
			if (list != null)
			{
				this.RecycleList(list);//重新压入空的到queue
            }
			return this.dictionary.Remove(t);
		}

		/// <summary>
		/// 不返回内部的list,copy一份出来
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public K[] GetAll(T t)
		{
			List<K> list;
			this.dictionary.TryGetValue(t, out list);
			if (list == null)
			{
				return new K[0];
			}
			return list.ToArray();
		}

		/// <summary>
		/// 返回内部的list
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public List<K> this[T t]
		{
			get
			{
				List<K> list;
				this.dictionary.TryGetValue(t, out list);
				return list;
			}
		}

        //获取第一个
		public K GetOne(T t)
		{
			List<K> list;
			this.dictionary.TryGetValue(t, out list);
			if (list != null && list.Count > 0)
			{
				return list[0];
			}
			return default(K);
		}

        //判断是否包含value
		public bool Contains(T t, K k)
		{
			List<K> list;
			this.dictionary.TryGetValue(t, out list);
			if (list == null)
			{
				return false;
			}
			return list.Contains(k);
		}

        //是否包含key
		public bool ContainsKey(T t)
		{
			return this.dictionary.ContainsKey(t);
		}

        //清空
		public void Clear()
		{
			dictionary.Clear();
		}
	}
}