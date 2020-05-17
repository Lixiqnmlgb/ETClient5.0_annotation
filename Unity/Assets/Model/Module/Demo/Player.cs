namespace ETModel
{
    /// <summary>
    /// 角色实体
    /// </summary>
	public sealed class Player : Entity
	{
		public long UnitId { get; set; }
		
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