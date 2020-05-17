using PF;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace ETModel
{
	[ObjectSystem]
	public class UnitAwakeSystem : AwakeSystem<Unit, GameObject>
	{
		public override void Awake(Unit self, GameObject gameObject)
		{
			self.Awake(gameObject);
		}
	}
	
    //在层级视图中隐藏
	[HideInHierarchy]
	public sealed class Unit: Entity
	{
		public void Awake(GameObject gameObject)
		{
			this.GameObject = gameObject;
			this.GameObject.AddComponent<ComponentView>().Component = this;
		}
		
        //位置
		public Vector3 Position
		{
			get
			{
				return GameObject.transform.position;
			}
			set
			{
				GameObject.transform.position = value;
			}
		}
        //旋转
		public Quaternion Rotation
		{
			get
			{
				return GameObject.transform.rotation;
			}
			set
			{
				GameObject.transform.rotation = value;
			}
		}
        //释放掉
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