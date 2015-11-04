using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace MiniJSON
{
	internal class Reflection
	{
		public static T GetObject<T>(object data)
		{
			var res = ReflectObject(data, typeof(T));

			if (res is T)
				return (T)res;
			else
				return default(T);
		}

		private static object ReflectObject(object obj, Type type)
		{
			object res = null;

			if (obj is List<object>)
			{
				res = Activator.CreateInstance(type);
				Type genType = type.GetGenericArguments()[0];
				var objList = obj as IList;
				foreach (var item in objList)
				{
					var prs = ReflectObject(item, genType);
					if (prs != null)
						(res as IList).Add(prs);
				}
			}
			else if (obj is IDictionary<string, object>)
			{
				res = CreateObject(obj as IDictionary<string, object>, type);
			}

			return res;
		}

		private static object CreateObject(IDictionary<string, object> keyValue, Type type)
		{
			object res = null;

			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
			var properties = type.GetProperties(flags);
			var fields = type.GetFields(flags);

			res = FormatterServices.GetUninitializedObject(type);
			foreach (var item in keyValue)
			{
				if (item.Value == null)
					continue;

				var valueFilled = false;

				foreach (PropertyInfo property in properties)
				{
					if (!property.CanWrite)
						continue;

					var infoName = property.Name;
					var attrs = property.GetCustomAttributes(true);
					foreach (object attr in attrs)
					{
						var at = attr as DataMemberAttribute;
						if (at != null)
						{
							if (!string.IsNullOrEmpty(at.Name))
								infoName = at.Name;
							break;
						}
					}

					if (infoName == item.Key)
					{
						var pType = property.PropertyType;
						var itemValue = ReflectObject(item.Value, pType);
						if (itemValue == null)
						{
							itemValue = item.Value;
							if (pType.IsEnum)
								itemValue = Enum.Parse(pType, itemValue.ToString());
							else
								itemValue = Convert.ChangeType(itemValue, pType);
						}
						property.SetValue(res, itemValue, null);
						valueFilled = true;
						break;
					}
				}
				if (valueFilled)
					continue;

				foreach (FieldInfo field in fields)
				{
					var infoName = field.Name;
					var attrs = field.GetCustomAttributes(true);
					foreach (object attr in attrs)
					{
						var at = attr as DataMemberAttribute;
						if (at != null)
						{
							if (!string.IsNullOrEmpty(at.Name))
								infoName = at.Name;
							break;
						}
					}

					if (infoName == item.Key)
					{
						var fType = field.FieldType;
						var itemValue = ReflectObject(item.Value, fType);
						if (itemValue == null)
						{
							itemValue = item.Value;
							if (fType.IsEnum)
								itemValue = Enum.Parse(fType, itemValue.ToString());
							else
								itemValue = Convert.ChangeType(itemValue, fType);
						}
						field.SetValue(res, itemValue);
						break;

					}
				}
			}
			return res;
		}
	}
}
