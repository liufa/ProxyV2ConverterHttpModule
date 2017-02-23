using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ProxyV2ConverterHttpModule
{
    public class XForwardedForRewriter : IHttpModule
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        byte[] proxyv2HeaderStartRequence = new byte[13] { 0x0D, 0x0A, 0x0D, 0x0A, 0x00, 0x0D, 0x0A, 0x51, 0x55, 0x49, 0x54, 0x0A , 0x02 };

        public void Init(HttpApplication context)
        {
            context.BeginRequest += Context_BeginRequest;
        }

        public Func<object, HttpRequestBase> GetRequest = (object sender) =>
        {
            return new HttpRequestWrapper(((HttpApplication)sender).Context.Request);
        };

        public void Context_BeginRequest(object sender, EventArgs e)
        {
            var request = GetRequest(sender);

            var proxyv2header = request.BinaryRead(13);
            if (!proxyv2header.SequenceEqual(proxyv2HeaderStartRequence))
            {
                request.Abort();
            }
            else
            {
                var proxyv2IpvType = request.BinaryRead(5).Skip(1).Take(1).Single();
                var isIpv4 = new byte[] { 0x11, 0x12 }.Contains(proxyv2IpvType);
                var ip = isIpv4 ? 
                    Regex.Replace(Encoding.ASCII.GetString(request.BinaryRead(12)), ".{3}", "$0.").TrimEnd(new[] { '.' }) : 
                    Encoding.ASCII.GetString(request.BinaryRead(36));

                var currentXForwardedFor = string.Empty;
                var headers = request.Headers;
                if (headers.Get("X-Forwarded-For") != null)
                    currentXForwardedFor = headers["X-Forwarded-For"];
                Type hdr = headers.GetType();
                PropertyInfo ro = hdr.GetProperty("IsReadOnly",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);

                ro.SetValue(headers, false, null);

                request.Headers.Remove("X-Forwarded-For");
                hdr.InvokeMember("InvalidateCachedArrays",
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, headers, null);

                hdr.InvokeMember("BaseAdd",
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, headers,
                    new object[] { "X-Forwarded-For", new ArrayList { !string.IsNullOrEmpty(currentXForwardedFor) ? $"{currentXForwardedFor},{ip}" : ip } });

                ro.SetValue(headers, true, null);
            }
        }
    }
}
