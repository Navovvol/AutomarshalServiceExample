using System;
using System.IO;
using System.Net;

namespace ListenServiceProject
{
	class Program
	{
		static void Main(string[] args)
		{
			var httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://127.0.0.1:49999/");
			httpListener.Start();

			while(true)
			{
				var context = httpListener.GetContext();
				var encoding = context.Request.ContentEncoding;

				using(var responce = context.Response)
				using(var inputStream = context.Request.InputStream)
				using(var reader = new StreamReader(inputStream, encoding))
				{
					var obj = reader.ReadToEnd();

					Console.WriteLine(obj);
					responce.Close(); // Обязательно закрываем! Метод возвращает ответ!
				}
			}
		}
	}
}
