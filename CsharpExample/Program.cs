using System;
using System.IO;
using System.Net;
using System.Text;

using Newtonsoft.Json;

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

			if(!IsSubscribed(req))  // если подписки нет - подписаться и слушать сообщения от сервиса
			{
				SetNewSubscription("http://127.0.0.1:49999");
			}

			ListenService("http://127.0.0.1:49999/");
		}

		/// <summary>Проверяет есть ли подписка.</summary>
		/// <param name="req">Запрос <see cref="HttpWebRequest"/>.</param>
		/// <returns><c>true</c> если подписка есть; иначе, <c>false</c>.</returns>
		public static bool IsSubscribed(HttpWebRequest req)
		{
			try
			{
				using(var resp = req.GetResponse() as HttpWebResponse)
				using(var respStream = resp.GetResponseStream())
				using(var sr = new StreamReader(respStream))
				{
					Console.WriteLine(resp.StatusCode);
					Console.Write(sr.ReadToEnd());
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
				DecisionComposition = new string[] { "Plate", "Channel" },
				ImagesComposition = new string[] { },
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
}
