using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ProxyV2ConverterHttpModule
{
    public class XForwardedForRewriter : IHttpModule
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        byte[] proxyv2HeaderStartRequence = new byte[12] { 0x0D, 0x0A, 0x0D, 0x0A, 0x00, 0x0D, 0x0A, 0x51, 0x55, 0x49, 0x54, 0x0A };

        public void Init(HttpApplication context)
        {
            context.BeginRequest += Context_BeginRequest;
        }

        public void Context_BeginRequest(object sender, EventArgs e)
        {
            var request = ((HttpApplication)sender).Context.Request;

            var proxyv2header = request.BinaryRead(12);
            if (!proxyv2header.SequenceEqual(proxyv2HeaderStartRequence))
            {
                request.Abort();
            }
            else
            {
                var proxyv2IpvType = request.BinaryRead(5).Skip(1).Take(1).Single();
                var isIpv4 = new byte[] { 0x11, 0x12 }.Contains(proxyv2IpvType);
                var ipInBinary = isIpv4 ? request.BinaryRead(12) : request.BinaryRead(36);
                var ip = Convert.ToString(ipInBinary);

                var headers = request.Headers;
                Type hdr = headers.GetType();
                PropertyInfo ro = hdr.GetProperty("IsReadOnly",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
                // Remove the ReadOnly property
                ro.SetValue(headers, false, null);
                // Invoke the protected InvalidateCachedArrays method 
                hdr.InvokeMember("InvalidateCachedArrays",
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, headers, null);
                // Now invoke the protected "BaseAdd" method of the base class to add the
                // headers you need. The header content needs to be an ArrayList or the
                // the web application will choke on it.
                hdr.InvokeMember("BaseAdd",
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, headers,
                    new object[] { "X-Forwarded-For", new ArrayList { ip } });
                // repeat BaseAdd invocation for any other headers to be added
                // Then set the collection back to ReadOnly
                ro.SetValue(headers, true, null);
            }
        }
    }
}
