using System;
using System.IO;
using System.Net;
using System.Text;

using Newtonsoft.Json;

namespace SetNewSubscriptionProject
{
	class Program
	{
		static void Main(string[] args)
		{
			var req = WebRequest.CreateHttp("http://127.0.0.1:9595/api/v1/subscriptions/new");
			req.ContentType = "application/json";
			req.Method = "POST";
			req.Accept = "*/*";

			var jsonBody = new SubscriptionJson();
			jsonBody.CallBack.Address = "http://127.0.0.1:49999";
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
			Console.ReadKey();
		}
	}

	public class CallBackProperty
	{
		[JsonProperty(PropertyName = "address")]
		public string Address { get; set; }

		[JsonProperty(PropertyName = "method")]
		public string Method { get; set; }
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
