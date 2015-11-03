/*
 * Copyright (c) 2013 Calvin Rien
 *
 * Based on the JSON parser by Patrick van Bergen
 * http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html
 *
 * Simplified it so that it doesn't throw exceptions
 * and can be used in Unity iPhone with maximum code stripping.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MiniJSON
{
	// Example usage:
	//
	//  using UnityEngine;
	//  using System.Collections;
	//  using System.Collections.Generic;
	//  using MiniJSON;
	//
	//  public class MiniJSONTest : MonoBehaviour {
	//      void Start () {
	//          var jsonString = "{ \"array\": [1.44,2,3], " +
	//                          "\"object\": {\"key1\":\"value1\", \"key2\":256}, " +
	//                          "\"string\": \"The quick brown fox \\\"jumps\\\" over the lazy dog \", " +
	//                          "\"unicode\": \"\\u3041 Men\u00fa sesi\u00f3n\", " +
	//                          "\"int\": 65536, " +
	//                          "\"float\": 3.1415926, " +
	//                          "\"bool\": true, " +
	//                          "\"null\": null }";
	//
	//          var dict = Json.Deserialize(jsonString) as Dictionary<string,object>;
	//
	//          Debug.Log("deserialized: " + dict.GetType());
	//          Debug.Log("dict['array'][0]: " + ((List<object>) dict["array"])[0]);
	//          Debug.Log("dict['string']: " + (string) dict["string"]);
	//          Debug.Log("dict['float']: " + (double) dict["float"]); // floats come out as doubles
	//          Debug.Log("dict['int']: " + (long) dict["int"]); // ints come out as longs
	//          Debug.Log("dict['unicode']: " + (string) dict["unicode"]);
	//
	//          var str = Json.Serialize(dict);
	//
	//          Debug.Log("serialized: " + str);
	//      }
	//  }

	/// <summary>
	/// This class encodes and decodes JSON strings.
	/// Spec. details, see http://www.json.org/
	///
	/// JSON uses Arrays and Objects. These correspond here to the datatypes IList and IDictionary.
	/// All numbers are parsed to doubles.
	/// </summary>
	public static class Json
	{
		/// <summary>
		/// Parses the string json into a value
		/// </summary>
		/// <param name="json">A JSON string.</param>
		/// <returns>An List&lt;object&gt;, a Dictionary&lt;string, object&gt;, a double, an integer,a string, null, true, or false</returns>
		public static object Deserialize(string json)
		{
			// save the string for debug information
			if (json == null)
			{
				return null;
			}

			return Parser.Parse(json);
		}

		private static bool _serialize = false;

		public static T Deserialize<T>(string json) where T : class
		{
			if (json == null)
				return default(T);

			var parse = Parser.Parse(json);

			var type = parse.GetType();
			var attrType = typeof(DataContractAttribute);
			var dataContactAttr = Attribute.GetCustomAttribute(type, attrType);
			_serialize = (dataContactAttr != null);

			var res = DeserializeObject(parse, typeof(T));

			return res as T;
		}


		private static object DeserializeObject(object obj, Type type)
		{
			object res = null;

			if (obj is List<object>)
			{
				res = Activator.CreateInstance(type);
				Type genType = type.GetGenericArguments()[0];
				var objList = obj as IList;
				foreach (var item in objList)
				{
					var prs = DeserializeObject(item, genType);
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

			var properties = type.GetProperties();

			res = Activator.CreateInstance(type);
			foreach (var item in keyValue)
			{
				if (item.Value == null)
					continue;

				foreach (var property in properties)
				{
					if (!property.CanWrite)
						continue;

					var infoName = property.Name.ToLower();
					var attrs = property.GetCustomAttributes(true);
					var skip = _serialize;

					foreach (object attr in attrs)
					{
						var at = attr as DataMemberAttribute;
						if (at != null)
						{
							if (!string.IsNullOrEmpty(at.Name))
								infoName = at.Name.ToLower();
							skip = false;
							break;
						}
					}

					if (skip)
						continue;

					if (infoName == item.Key.ToLower())
					{
						var itemValue = DeserializeObject(item.Value, property.PropertyType) ?? item.Value;
						property.GetSetMethod().Invoke(res, new object[] { itemValue });
						break;
					}
				}
			}
			return res;
		}

		public static string Serialize(object obj)
		{
			return Serializer.Serialize(obj);
		}
	}
}