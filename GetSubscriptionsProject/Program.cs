using System;
using System.IO;
using System.Net;

namespace GetSubscriptionsProject
{
	class Program
	{
		static void Main(string[] args)
		{
			var req = WebRequest.CreateHttp("http://127.0.0.1:9595/api/v1/subscriptions");
			req.Method = "GET";
			req.Accept = "*/*";
			var resp = (HttpWebResponse)req.GetResponse();
			using(var respStream = resp.GetResponseStream())
			using(var sr = new StreamReader(respStream))
			{
				Console.WriteLine(resp.StatusCode);
				Console.Write(sr.ReadToEnd());
			}
			Console.ReadKey();
		}
	}
}
