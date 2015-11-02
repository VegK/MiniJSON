using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace MiniJSON
{
	internal sealed class Serializer
	{
		StringBuilder builder;

		Serializer()
		{
			builder = new StringBuilder();
		}

		public static string Serialize(object obj)
		{
			var instance = new Serializer();

			instance.SerializeValue(obj);

			return instance.builder.ToString();
		}

		void SerializeValue(object value)
		{
			IList asList;
			IDictionary asDict;
			string asStr;

			if (value == null)
			{
				builder.Append("null");
			}
			else if ((asStr = value as string) != null)
			{
				SerializeString(asStr);
			}
			else if (value is bool)
			{
				builder.Append((bool)value ? "true" : "false");
			}
			else if ((asList = value as IList) != null)
			{
				SerializeArray(asList);
			}
			else if ((asDict = value as IDictionary) != null)
			{
				SerializeObject(asDict);
			}
			else if (value is char)
			{
				SerializeString(new string((char)value, 1));
			}
			else
			{
				SerializeOther(value);
			}
		}

		void SerializeObject(IDictionary obj)
		{
			bool first = true;

			builder.Append('{');

			foreach (object e in obj.Keys)
			{
				if (!first)
				{
					builder.Append(',');
				}

				SerializeString(e.ToString());
				builder.Append(':');

				SerializeValue(obj[e]);

				first = false;
			}

			builder.Append('}');
		}

		void SerializeArray(IList anArray)
		{
			builder.Append('[');

			bool first = true;

			foreach (object obj in anArray)
			{
				if (!first)
				{
					builder.Append(',');
				}

				SerializeValue(obj);

				first = false;
			}

			builder.Append(']');
		}

		void SerializeString(string str)
		{
			builder.Append('\"');

			char[] charArray = str.ToCharArray();
			foreach (var c in charArray)
			{
				switch (c)
				{
					case '"':
						builder.Append("\\\"");
						break;
					case '\\':
						builder.Append("\\\\");
						break;
					case '\b':
						builder.Append("\\b");
						break;
					case '\f':
						builder.Append("\\f");
						break;
					case '\n':
						builder.Append("\\n");
						break;
					case '\r':
						builder.Append("\\r");
						break;
					case '\t':
						builder.Append("\\t");
						break;
					default:
						int codepoint = Convert.ToInt32(c);
						if ((codepoint >= 32) && (codepoint <= 126))
						{
							builder.Append(c);
						}
						else
						{
							builder.Append("\\u");
							builder.Append(codepoint.ToString("x4"));
						}
						break;
				}
			}

			builder.Append('\"');
		}

		void SerializeOther(object value)
		{
			// NOTE: decimals lose precision during serialization.
			// They always have, I'm just letting you know.
			// Previously floats and doubles lost precision too.
			if (value is float)
			{
				builder.Append(((float)value).ToString("R"));
			}
			else if (value is int
			  || value is uint
			  || value is long
			  || value is sbyte
			  || value is byte
			  || value is short
			  || value is ushort
			  || value is ulong)
			{
				builder.Append(value);
			}
			else if (value is double
			  || value is decimal)
			{
				builder.Append(Convert.ToDouble(value).ToString("R"));
			}
			else
			{
				if (!SerializeClass(value))
					SerializeString(value.ToString());
			}
		}

		bool SerializeClass(object value)
		{
			Type t = value.GetType();

			var dataContactAttr = Attribute.GetCustomAttribute(t, typeof(DataContractAttribute));
			bool serializable = (dataContactAttr != null);

			var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			var objects = new Dictionary<string, object>();

			for (int i = 0; i < properties.Length; i++)
			{
				var item = properties[i];
				var itemName = item.Name;
				var skip = serializable;

				if (serializable)
				{
					var attrs = item.GetCustomAttributes(true);
					foreach (object attr in attrs)
					{
						var at = attr as DataMemberAttribute;
						if (at != null)
						{
							if (!string.IsNullOrEmpty(at.Name))
								itemName = at.Name;
							skip = false;
							break;
						}
					}
				}

				if (skip)
					continue;

				var itemValue = item.GetGetMethod().Invoke(value, null);
				objects.Add(itemName, itemValue);
			}

			var res = (objects.Count > 0);
			if (res)
				builder.Append(Serialize(objects));
			return res;
		}
	}
}
