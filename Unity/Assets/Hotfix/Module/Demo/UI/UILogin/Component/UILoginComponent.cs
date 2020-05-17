using System;
using System.Net;
using ETModel;
using UnityEngine;
using UnityEngine.UI;

namespace ETHotfix
{
	[ObjectSystem]
	public class UiLoginComponentSystem : AwakeSystem<UILoginComponent>
	{
		public override void Awake(UILoginComponent self)
		{
			//在此填充Awake的逻辑
			//可以再调用UILoginComponent的Awake方法
			self.Awake();
		}
	}

	[ObjectSystem]
	public class UiLoginComponentStartSystem : StartSystem<UILoginComponent>
	{
		public override void Start(UILoginComponent self)
		{
			Log.Debug("在此实现Start的逻辑!");
		}
	}


	public class UILoginComponent: Component
	{
		private GameObject account;
		private GameObject loginBtn;

		public void Awake()
		{
			ReferenceCollector rc = this.GetParent<UI>().GameObject.GetComponent<ReferenceCollector>();
			loginBtn = rc.Get<GameObject>("LoginBtn");
			loginBtn.GetComponent<Button>().onClick.Add(OnLogin);
			this.account = rc.Get<GameObject>("Account");
		}

		public void OnLogin()
		{
			LoginHelper.OnLoginAsync(this.account.GetComponent<InputField>().text).Coroutine();
		}
	}
}
