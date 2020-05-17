using System.Collections.Generic;
using System.Linq;

namespace ETModel
{
	[ObjectSystem]
	public class UnitComponentSystem : AwakeSystem<UnitComponent>
	{
		public override void Awake(UnitComponent self)
		{
			self.Awake();
		}
	}

    /// <summary>
    /// 单位组件
    /// </summary>
    public class UnitComponent: Component
	{
		public static UnitComponent Instance { get; private set; }

		public Unit MyUnit;
		//缓存起来的单位实体
		private readonly Dictionary<long, Unit> idUnits = new Dictionary<long, Unit>();

		public void Awake()
		{
			Instance = this;
		}

        //释放
		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			base.Dispose();
            //将所有单位都释放掉
			foreach (Unit unit in this.idUnits.Values)
			{
				unit.Dispose();
			}

			this.idUnits.Clear();

			Instance = null;
		}

        //添加
		public void Add(Unit unit)
		{
			this.idUnits.Add(unit.Id, unit);
			unit.Parent = this;
		}
        //获取
		public Unit Get(long id)
		{
			Unit unit;
			this.idUnits.TryGetValue(id, out unit);
			return unit;
		}
        //移除
		public void Remove(long id)
		{
			Unit unit;
			this.idUnits.TryGetValue(id, out unit);
			this.idUnits.Remove(id);
			unit?.Dispose();
		}
        //移除但是不释放
		public void RemoveNoDispose(long id)
		{
			this.idUnits.Remove(id);
		}
        //数量
		public int Count
		{
			get
			{
				return this.idUnits.Count;
			}
		}
        //获取所有单位
		public Unit[] GetAll()
		{
			return this.idUnits.Values.ToArray();
		}
	}
}