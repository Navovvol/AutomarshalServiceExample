using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CsharpExample
{
	class Program
	{
		// Проверяем есть ли подписка на адрес клиента "http://127.0.0.1:49999 .
		// Если подписки нет - подписываем, потом слушаем сообщения от сервиса ("http://127.0.0.1:9595")
		// Если подписка уже есть - слушаем сообщения от сервиса ("http://127.0.0.1:9595")
		// http://127.0.0.1 - IP - адрес сервиса
		// 9595 - порт сервиса

		static void Main(string[] args)
		{
			// Отправим на "http://127.0.0.1:9595/api/v1/subscriptions" POST запрос с фильтром, где укажем адрес получателя "http://127.0.0.1:49999"

			var req = WebRequest.CreateHttp("http://127.0.0.1:9595/api/v1/subscriptions");
			req.Method = "POST";
			req.Accept = "*/*";
			req.ContentType = "application/json";

			var filter = new CallBackFilter();
			filter.CallBack.Address = "http://127.0.0.1:49999";
			filter.Type = "recognition";

			var serObj = JsonConvert.SerializeObject(filter);
			var body = Encoding.UTF8.GetBytes(serObj);
			req.ContentLength = body.Length;

			using(var sw = req.GetRequestStream())
			{
				sw.Write(body, 0, body.Length);
			}

			if(!IsSubscribed(req, out var oldSessions))  // если подписки нет - подписаться и слушать сообщения от сервиса
			{
				SetNewSubscription("http://127.0.0.1:49999");
			}
			else // Если подписка есть - отписаться и подписаться заново
			{

				Console.WriteLine();
				Console.WriteLine("OldSessions:");
				foreach(var session in oldSessions)
				{
					Console.WriteLine();
					Console.WriteLine(session);

					DeleteOldSubscription(session);
				}
			}

			ListenService("http://127.0.0.1:49999/");
		}

		/// <summary>Проверяет есть ли подписка.</summary>
		/// <param name="req">Запрос <see cref="HttpWebRequest"/>.</param>
		/// <param name="idSessions">Список подписок на сервис.</param>
		/// <returns><c>true</c> если подписка есть; иначе, <c>false</c>.</returns>
		public static bool IsSubscribed(HttpWebRequest req, out List<string> idSessions)
		{
			idSessions = new List<string>();
			try
			{
				using(var resp = req.GetResponse() as HttpWebResponse)
				using(var respStream = resp.GetResponseStream())
				using(var sr = new StreamReader(respStream))
				{
					Console.WriteLine(resp.StatusCode);
					var body = sr.ReadToEnd();
					Console.Write(body);

					var jArray = JArray.Parse(body);
					foreach(var jobj in jArray)
					{
						var ids = jobj["id"];
						idSessions.Add(ids.Value<string>());
					}
				}

				return true;
			}
			catch(WebException exc) when((exc.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
			{
				return false;
			}
			catch(Exception exc)
			{
				Console.WriteLine(exc);
				return false;
			}
		}

		/// <summary>Создает новую подписку.</summary>
		/// <param name="address">Адрес клиента.</param>
		public static void SetNewSubscription(string address)
		{
			var req = WebRequest.CreateHttp("http://127.0.0.1:9595/api/v1/subscriptions/new");
			req.ContentType = "application/json";
			req.Method = "POST";
			req.Accept = "*/*";

			var jsonBody = new SubscriptionJson();
			jsonBody.CallBack.Address = address;
			jsonBody.CallBack.Method = "POST";
			jsonBody.Type = "recognition";

			var filter = new FilterProperty
			{
				Channels = new int[] { 0, 1, 2, 3 },
				DecisionComposition = new string[] { "Plate", "Channel", "Timestamps", "Movement", "PlateBounds" },
				//DecisionComposition = new string[] { "Plate", "Channel", "Timestamps", "Bounds", "Movement", "PlateBounds" },
				ImagesComposition = new string[] { "DecisionFrame", "LinkedFrames", "NumberPlateImage" },
				//ImagesComposition = new string[] { },
			};

			jsonBody.Filter = JsonConvert.SerializeObject(filter);

			var serObj = JsonConvert.SerializeObject(jsonBody);
			var body = Encoding.UTF8.GetBytes(serObj);
			req.ContentLength = body.Length;

			using(var sw = req.GetRequestStream())
			{
				sw.Write(body, 0, body.Length);
			}

			using(var resp = (HttpWebResponse)req.GetResponse())
			using(var respStream = resp.GetResponseStream())
			using(var sr = new StreamReader(respStream))
			{
				Console.WriteLine(resp.StatusCode);
				Console.Write(sr.ReadToEnd());
			}
		}

		public static void DeleteOldSubscription(string oldSessionId)
		{
			var req = WebRequest.CreateHttp($@"http://127.0.0.1:9595/api/v1/subscriptions/subscription?id={oldSessionId}");
			req.ContentType = "application/json";
			req.Method = "DELETE";
			req.Accept = "*/*";

			using(var resp = (HttpWebResponse)req.GetResponse())
			using(var respStream = resp.GetResponseStream())
			using(var sr = new StreamReader(respStream))
			{
				Console.WriteLine(resp.StatusCode);
				Console.Write(sr.ReadToEnd());
			}
		}
		/// <summary>Слушает сервис распознавания.</summary>
		/// <param name="address">Адрес клиента.</param>
		public static void ListenService(string address)
		{
			var httpListener = new HttpListener();
			httpListener.Prefixes.Add(address);
			httpListener.Start();

			while(true)
			{
				var context = httpListener.GetContext();
				var encoding = Encoding.UTF8;

				using(var responce = context.Response)
				using(var inputStream = context.Request.InputStream)
				using(var reader = new StreamReader(inputStream, encoding))
				{
					var obj = reader.ReadToEnd();
					Console.WriteLine(obj);
					var splitStrings = obj.Split(System.Environment.NewLine.ToCharArray());

					var body = JsonConvert.DeserializeObject<Decision>(splitStrings[6]);
					Console.WriteLine(body.ToString());

					responce.StatusCode = 200;
					responce.Close();	//обязательно закрываем! Метод отправляет ответ!
				}
			}
		}
	}
	
	public class CallBackProperty
	{
		[JsonProperty(PropertyName = "address")]
		public string Address { get; set; }

		[JsonProperty(PropertyName = "method")]
		public string Method { get; set; }
	}

	public class CallBackFilter
	{
		[JsonProperty(PropertyName = "id")]
		public string Id { get; set; }

		[JsonProperty(PropertyName = "callback")]
		public CallBackProperty CallBack { get; }

		[JsonProperty(PropertyName = "type")]
		public string Type { get; set; }

		[JsonProperty(PropertyName = "isPermanent")]
		public bool IsPermanent { get; set; }

		public CallBackFilter()
		{
			CallBack = new CallBackProperty();
		}
	}

	public class FilterProperty
	{
		[JsonProperty(PropertyName = "channels")]
		public int[] Channels { get; set; }

		[JsonProperty(PropertyName = "decisionСomposition")]
		public string[] DecisionComposition { get; set; }

		[JsonProperty(PropertyName = "imagesСomposition")]
		public string[] ImagesComposition { get; set; }

		[JsonProperty(PropertyName = "isBlocked")]
		public bool IsBlocked { get; set; }
	}

	public class SubscriptionJson
	{
		[JsonProperty(PropertyName = "callback")]
		public CallBackProperty CallBack { get; set; }

		[JsonProperty(PropertyName = "type")]
		public string Type { get; set; }

		[JsonProperty(PropertyName = "filter")]
		public string Filter { get; set; }

		public SubscriptionJson()
		{
			CallBack = new CallBackProperty();
		}
	}

	public class Decision : IItem
	{
		[JsonProperty(PropertyName = "tokens")]
		public string Tokens { get; set; }

		[JsonProperty(PropertyName = "id")] public long Id { get; set; }

		[JsonProperty(PropertyName = "channel")]
		public Channel Channel { get; set; }

		[JsonProperty(PropertyName = "timestamps")]
		public TimeStamps Timestamps { get; set; }

		[JsonProperty(PropertyName = "plate")] public PlateInfo Plate { get; set; }

		public override string ToString() => PropertyHelper.GetAllPropertiesValues(this);
	}

	public class Channel : IItem
	{
		[JsonProperty(PropertyName = "groupId")]
		public object GroupId { get; set; }

		[JsonProperty(PropertyName = "id")]
		public long Id { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }
	}

	public class TimeStamps : IItem
	{
		[JsonProperty(PropertyName = "bestFrame")]
		public DateTime BestFrame { get; set; }

		[JsonProperty(PropertyName = "firstFrame")]
		public DateTime FirstFrame { get; set; }

		[JsonProperty(PropertyName = "lastFrame")]
		public DateTime LastFrame { get; set; }
	}

	public class PlateInfo : IItem
	{
		[JsonProperty(PropertyName = "number")]
		public string Number { get; set; }

		[JsonProperty(PropertyName = "confidence")]
		public long Confidence { get; set; }

		[JsonProperty(PropertyName = "stencil")]
		public string Stencil { get; set; }
	}

	public interface IItem
	{}

	public static class PropertyHelper
	{
		public static string GetAllPropertiesValues(object src)
		{
			var str = new StringBuilder();
			if(src is IItem)
			{
				var properties = src?.GetType()?.GetProperties();

				foreach(var propertyInfo in properties)
				{
					var propValue = propertyInfo?.GetValue(src);
					if(propValue?.GetType()?.GetProperties()?.Length != 0)
					{
						str.Append(GetAllPropertiesValues(propValue));
					}

					if(propValue is IItem) continue;

					str.AppendLine($@"{propertyInfo.Name}:{propValue}");
				}
			}
			
			return str.ToString();
		}
	}
}
