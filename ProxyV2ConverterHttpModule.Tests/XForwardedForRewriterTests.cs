using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyV2ConverterHttpModule.Tests
{
    [TestFixture]
    public class XForwardedForRewriterTests
    {
        [Test]
        public void BeginRequest_HeaderHasBeenRemoved()
        {
            var sut = new XForwardedForRewriter();
            sut.Context_BeginRequest(new object(), new EventArgs());
        }
    }
}
