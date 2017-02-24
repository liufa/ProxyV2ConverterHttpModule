using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    public class Program
    {

        static byte[] proxyv2HeaderStartRequence = new byte[13] { 0x0D, 0x0A, 0x0D, 0x0A, 0x00, 0x0D, 0x0A, 0x51, 0x55, 0x49, 0x54, 0x0A, 0x02 };

        public static void Main()
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");

                Console.WriteLine("Starting TCP listener...");


                TcpListener listener = new TcpListener(ipAddress, 500);

                listener.Start();

                while (true)
                {
                    Console.WriteLine("Server is listening on " + listener.LocalEndpoint);

                    Console.WriteLine("Waiting for a connection...");

                    Socket socket = listener.AcceptSocket();

                    Console.WriteLine("Connection accepted.");


                    var childSocketThread = new Thread(() =>
                    {

                        var getOrPostHttp1Acc = new List<byte>();
                        var getOrPostHttp1 = new byte[1];
                        while (getOrPostHttp1[0] != 10)
                        {
                            socket.Receive(getOrPostHttp1);
                            getOrPostHttp1Acc.Add(getOrPostHttp1[0]);
                        }
                        bool isGet = Encoding.ASCII.GetString(getOrPostHttp1Acc.ToArray()).ToUpper().Contains("GET");
                        byte[] proxyv2headerIdentifier = new byte[13];
                        socket.Receive(proxyv2headerIdentifier);
                        var proxyv2header = new List<byte>();
                        var proxyv2HeaderBuffer = new byte[1];
                        if (proxyv2headerIdentifier.SequenceEqual(proxyv2HeaderStartRequence))
                        {
                            while (proxyv2HeaderBuffer[0] != 10)
                            {
                                socket.Receive(proxyv2HeaderBuffer);
                                proxyv2header.Add(proxyv2HeaderBuffer[0]);
                            }

                            string headerString = $"proxyv2:{Encoding.ASCII.GetString(proxyv2header.ToArray())}";
                            byte[] bodyBuff = new byte[0];
                            byte[] buffer = new byte[1];
                            int contentLength = 0;
                            while (!headerString.Contains("\r\n\r\n"))
                            {
                                socket.Receive(buffer, 0, 1, 0);
                                headerString += Encoding.ASCII.GetString(buffer);
                            }
                            var body = string.Empty;
                            if (!isGet)
                            {
                                Regex reg = new Regex("\\\r\nContent-Length: (.*?)\\\r\n");
                                Match m = reg.Match(headerString);
                                contentLength = int.Parse(m.Groups[1].ToString());
                                bodyBuff = new byte[contentLength];
                                socket.Receive(bodyBuff, 0, contentLength, 0);
                                body = Encoding.ASCII.GetString(bodyBuff);
                            }

                            RedirectRequest(contentLength, body, headerString, isGet);
                        }

                        Console.WriteLine();

                        socket.Close();
                    });

                    childSocketThread.Start();

                }

                listener.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.StackTrace);
                Console.ReadLine();
            }
        }

        public static void RedirectRequest(int length, string body, string headers, bool IsGet)
        {
            var request = (HttpWebRequest)WebRequest.Create("http://localhost:53566/");
            var headersAsKeyValuePairs = headers.Split(new[] { "\r\n" }, StringSplitOptions.None);

            foreach (var header in headersAsKeyValuePairs)
            {
                var split = header.Split(new[] { ':' });
                if (split.Length > 1 && split[0].ToUpper() != "HOST")
                    request.SetRawHeader(split[0], split[1]);
            }

            if (IsGet)
            {
                request.Method = "GET";

            }
            else
            {
                var data = Encoding.ASCII.GetBytes(body);

                request.Method = "POST";
                request.ContentLength = length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, length);
                }
            }

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
        }

    }
    public static class HttpWebRequestExtensions
    {
        static string[] RestrictedHeaders = new string[] {
            "Accept",
            "Connection",
            "Content-Length",
            "Content-Type",
            "Date",
            "Expect",
            "Host",
            "If-Modified-Since",
            "Keep-Alive",
            "Proxy-Connection",
            "Range",
            "Referer",
            "Transfer-Encoding",
            "User-Agent"
    };

        static Dictionary<string, PropertyInfo> HeaderProperties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        static HttpWebRequestExtensions()
        {
            Type type = typeof(HttpWebRequest);
            foreach (string header in RestrictedHeaders)
            {
                string propertyName = header.Replace("-", "");
                PropertyInfo headerProperty = type.GetProperty(propertyName);
                HeaderProperties[header] = headerProperty;
            }
        }

        public static void SetRawHeader(this HttpWebRequest request, string name, string value)
        {
            if (HeaderProperties.ContainsKey(name))
            {
                PropertyInfo property = HeaderProperties[name];
                if (property.PropertyType == typeof(DateTime))
                    property.SetValue(request, DateTime.Parse(value), null);
                else if (property.PropertyType == typeof(bool))
                    property.SetValue(request, Boolean.Parse(value), null);
                else if (property.PropertyType == typeof(long))
                    property.SetValue(request, Int64.Parse(value), null);
                else
                    property.SetValue(request, value, null);
            }
            else
            {
                request.Headers[name] = value;
            }
        }
    }
}
