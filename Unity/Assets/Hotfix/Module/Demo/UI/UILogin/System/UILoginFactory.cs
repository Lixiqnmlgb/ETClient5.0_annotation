using System;
using ETModel;
using UnityEngine;

namespace ETHotfix
{
    public static class UILoginFactory
    {
        public static UI Create()
        {
	        try
	        {
				//获取主工程的ResourcesComponent组件
				ResourcesComponent resourcesComponent = ETModel.Game.Scene.
					GetComponent<ResourcesComponent>();
				//加载AB
				resourcesComponent.LoadBundle(UIType.UILogin.StringToAB());
				//获取资源
				GameObject bundleGameObject = (GameObject)resourcesComponent.
					GetAsset(UIType.UILogin.StringToAB(), UIType.UILogin);
				//克隆物体
				GameObject gameObject = UnityEngine.Object.Instantiate(bundleGameObject);

				//创建实体,并且内部调用了Awake方法
		        UI ui = ComponentFactory.Create<UI, string, GameObject>(UIType.UILogin, gameObject, false);
				//给实体增加组件
				ui.AddComponent<UILoginComponent>();
				return ui;
	        }
	        catch (Exception e)
	        {
				Log.Error(e);
		        return null;
	        }
		}
    }
}