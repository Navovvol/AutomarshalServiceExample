using System;
using System.IO;
using System.Net;
using System.Text;

namespace BeginListenServiceProject
{
	class Program
	{
		static void Main(string[] args)
		{
			var httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://127.0.0.1:49999/");
			httpListener.Start();
			httpListener.BeginGetContext(OnGotPendingRequest, httpListener);
			Console.ReadKey();
		}

		private static async void OnGotPendingRequest(IAsyncResult ar)
		{
			var httpListener = (HttpListener)ar.AsyncState;
			HttpListenerContext context;

			try
			{
				context = httpListener.EndGetContext(ar);
			}
			catch(ObjectDisposedException exc)
			{
				Console.WriteLine(exc);
				return;
			}

			try
			{
				using(var responce = context.Response)
				using(var inputStream = context.Request.InputStream)
				using(var reader = new StreamReader(inputStream, Encoding.UTF8))
				{
					var obj = await reader.ReadToEndAsync();
					Console.WriteLine();
					Console.WriteLine(obj);
					responce.Close(); //обязательно закрываем! Метод отправляет ответ!
				}
			}
			catch(Exception exc)
			{
				Console.WriteLine(exc);
			}

			if(httpListener.IsListening)
			{
				try
				{
					httpListener.BeginGetContext(OnGotPendingRequest, httpListener);
				}
				catch(InvalidOperationException exc)
				{
					Console.WriteLine(exc);
				}
			}
		}
	}
}
