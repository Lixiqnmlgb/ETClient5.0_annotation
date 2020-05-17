using System.Collections.Generic;
using System.Linq;

namespace ETModel
{
	[ObjectSystem]
	public class PlayerComponentAwakeSystem : AwakeSystem<PlayerComponent>
	{
		public override void Awake(PlayerComponent self)
		{
			self.Awake();
		}
	}
	
    /// <summary>
    /// 玩家组件,单例模式
    /// </summary>
	public class PlayerComponent : Component
	{
		public static PlayerComponent Instance { get; private set; }

		private Player myPlayer;

		public Player MyPlayer
		{
			get
			{
				return this.myPlayer;
			}
			set
			{
				this.myPlayer = value;
				this.myPlayer.Parent = this;
			}
		}

        //字典 玩家的缓存 
        private readonly Dictionary<long, Player> idPlayers = new Dictionary<long, Player>();

		public void Awake()
		{
			Instance = this;
		}
		
        //添加
		public void Add(Player player)
		{
			this.idPlayers.Add(player.Id, player);
			player.Parent = this;
		}

        //获取
		public Player Get(long id)
		{
			Player player;
			this.idPlayers.TryGetValue(id, out player);
			return player;
		}

        //移除
		public void Remove(long id)
		{
			this.idPlayers.Remove(id);
		}

        //数量
		public int Count
		{
			get
			{
				return this.idPlayers.Count;
			}
		}

        //获取所有玩家
		public Player[] GetAll()
		{
			return this.idPlayers.Values.ToArray();
		}

        //释放
		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			base.Dispose();

			foreach (Player player in this.idPlayers.Values)
			{
                //调用它自身释放的接口
				player.Dispose();
			}

			Instance = null;
		}
	}
}