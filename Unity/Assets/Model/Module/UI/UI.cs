using System.Collections.Generic;
using ETModel;
using UnityEngine;

namespace ETModel
{
	[ObjectSystem]
	public class UiAwakeSystem : AwakeSystem<UI, string, GameObject>
	{
		public override void Awake(UI self, string name, GameObject gameObject)
		{

			self.Awake(name, gameObject);
		}
	}
	
    /// <summary>
    /// UI实体
    /// </summary>
	[HideInHierarchy]
	public sealed class UI: Entity
	{
		public string Name { get; private set; }

        //子物体
		public Dictionary<string, UI> children = new Dictionary<string, UI>();
		
        //初始化:名称 物体,缓存到字典中
		public void Awake(string name, GameObject gameObject)
		{
			this.children.Clear();
			gameObject.AddComponent<ComponentView>().Component = this;
            //层级设置为UI
			gameObject.layer = LayerMask.NameToLayer(LayerNames.UI);
			this.Name = name;
			this.GameObject = gameObject;
		}

        //释放接口
		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			base.Dispose();

			foreach (UI ui in this.children.Values)
			{
				ui.Dispose();
			}
			
			UnityEngine.Object.Destroy(GameObject);
			children.Clear();
		}
        //设置为第一个
		public void SetAsFirstSibling()
		{
			this.GameObject.transform.SetAsFirstSibling();
		}
        //添加子物体
		public void Add(UI ui)
		{
			this.children.Add(ui.Name, ui);
			ui.Parent = this;
		}
        //移除
		public void Remove(string name)
		{
			UI ui;
			if (!this.children.TryGetValue(name, out ui))
			{
				return;
			}
			this.children.Remove(name);
			ui.Dispose();
		}
        //获取
		public UI Get(string name)
		{
			UI child;
            //如果缓存中包含直接返回
			if (this.children.TryGetValue(name, out child))
			{
				return child;
			}
            //否则进行遍历然后返回
			GameObject childGameObject = this.GameObject.transform.Find(name)?.gameObject;
			if (childGameObject == null)
			{
				return null;
			}
            //组件工厂进行创建 再进行返回 创建的时候 携带2个参数 调用Awake方法
			child = ComponentFactory.Create<UI, string, GameObject>(name, childGameObject);
            //压入缓存 再返回出去
			this.Add(child);
			return child;
		}
	}
}