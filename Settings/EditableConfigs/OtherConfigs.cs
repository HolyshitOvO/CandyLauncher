﻿using System.Reflection;
using ReflectSettings.Attributes;

namespace ReflectSettings.EditableConfigs
{
	public class OtherJustButton : EditableConfigBase<ButtonClick>
	{
		protected override ButtonClick ParseValue(object value)
		{
			// 这里可以获取到注解定义的东西
			return null;
		}

		public OtherJustButton(object forInstance, PropertyInfo propertyInfo, SettingsFactory factory, ChangeTrackingManager changeTrackingManager) : base(forInstance, propertyInfo, factory, changeTrackingManager)
		{
			// parse the existing value on the instance
			//Value = Value;
		}
	}
	
	public class OtherGroupTab : EditableConfigBase<ButtonClick>
	{
		protected override ButtonClick ParseValue(object value)
		{
			// 这里可以获取到注解定义的东西
			return null;
		}

		public OtherGroupTab(object forInstance, PropertyInfo propertyInfo, SettingsFactory factory, ChangeTrackingManager changeTrackingManager) : base(forInstance, propertyInfo, factory, changeTrackingManager)
		{
			// parse the existing value on the instance
			//Value = Value;
		}
	}
	
}
