using Moq;
using NUnit.Framework;
using System;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace ProxyV2ConverterHttpModule.Tests
{
    [TestFixture]
    public class XForwardedForRewriterTests
    {

        [Test]
        public void Request_Should_Abort()
        {
            //Arrange
            var request = Mock.Of<HttpRequestBase>();

            var sut = new XForwardedForRewriter();
            //replace with mock request for test
            sut.GetRequest = (object sender) => request;

            //Act
            sut.Context_BeginRequest(new object(), EventArgs.Empty);

            //Assert
            var mockRequest = Mock.Get(request);
            mockRequest.Verify(m => m.Abort(), Times.AtLeastOnce);
        }


        [Test]
        public void Request_Should_Forward()
        {
            //Arrange
            var request = Mock.Of<HttpRequestBase>();

            var mockRequest = Mock.Get(request);
            //setup mocked request with desired behavior for test
            var proxyv2HeaderStartRequence = new byte[12] { 0x0D, 0x0A, 0x0D, 0x0A, 0x00, 0x0D, 0x0A, 0x51, 0x55, 0x49, 0x54, 0x0A };
            mockRequest
                .Setup(m => m.BinaryRead(12))
                .Returns(proxyv2HeaderStartRequence);
            var ipaddress = Encoding.ASCII.GetBytes("192168255255-");
            mockRequest
                .Setup(m => m.BinaryRead(13))
                .Returns(ipaddress);
            var fakeProxyv2IpvType = new byte[5] { 0x00, 0x12, 0x00, 0x00, 0x00 };
            mockRequest
                .Setup(m => m.BinaryRead(5))
                .Returns(fakeProxyv2IpvType);

            var headers = new NameValueCollection();
            mockRequest.Setup(m => m.Headers).Returns(headers);

            var sut = new XForwardedForRewriter();

            sut.GetRequest = (object sender) => request;

            sut.Context_BeginRequest(new object(), EventArgs.Empty);

            var xForwardedFor = headers["X-Forwarded-For"];
            Assert.AreEqual(xForwardedFor, "192.168.255.255");
        }
        [Test]
        public void Request_Should_Overwrite()
        {
            //Arrange
            var request = Mock.Of<HttpRequestBase>();

            var mockRequest = Mock.Get(request);
            //setup mocked request with desired behavior for test
            var proxyv2HeaderStartRequence = new byte[12] { 0x0D, 0x0A, 0x0D, 0x0A, 0x00, 0x0D, 0x0A, 0x51, 0x55, 0x49, 0x54, 0x0A };
            mockRequest
                .Setup(m => m.BinaryRead(12))
                .Returns(proxyv2HeaderStartRequence);
            var ipaddress = Encoding.ASCII.GetBytes("192168255255-");
            mockRequest
                .Setup(m => m.BinaryRead(13))
                .Returns(ipaddress);
            var fakeProxyv2IpvType = new byte[5] { 0x00, 0x12, 0x00, 0x00, 0x00 };
            mockRequest
                .Setup(m => m.BinaryRead(5))
                .Returns(fakeProxyv2IpvType);

            var headers = new NameValueCollection { { "X-Forwarded-For", "123.132.132.123" } };
            mockRequest.Setup(m => m.Headers).Returns(headers);

            var sut = new XForwardedForRewriter();

            sut.GetRequest = (object sender) => request;

            sut.Context_BeginRequest(new object(), EventArgs.Empty);

            var xForwardedFor = headers["X-Forwarded-For"];
            Assert.AreEqual(xForwardedFor, "123.132.132.123,192.168.255.255");
        }

        [Test]
        public void Request_Should_ForwardIpv6()
        {
            //Arrange
            var request = Mock.Of<HttpRequestBase>();

            var mockRequest = Mock.Get(request);
            //setup mocked request with desired behavior for test
            var proxyv2HeaderStartRequence = new byte[12] { 0x0D, 0x0A, 0x0D, 0x0A, 0x00, 0x0D, 0x0A, 0x51, 0x55, 0x49, 0x54, 0x0A };
            mockRequest
                .Setup(m => m.BinaryRead(12))
                .Returns(proxyv2HeaderStartRequence);
            var ipaddress = Encoding.ASCII.GetBytes("2a01:e35:aaa4:6860:a5e7:5ba9:965e:cc93");
            mockRequest
                .Setup(m => m.BinaryRead(36))
                .Returns(ipaddress);
            var fakeProxyv2IpvType = new byte[5] { 0x00, 0x21, 0x00, 0x00, 0x00 };
            mockRequest
                .Setup(m => m.BinaryRead(5))
                .Returns(fakeProxyv2IpvType);

            var headers = new NameValueCollection();
            mockRequest.Setup(m => m.Headers).Returns(headers);

            var sut = new XForwardedForRewriter();

            sut.GetRequest = (object sender) => request;

            sut.Context_BeginRequest(new object(), EventArgs.Empty);

            var xForwardedFor = headers["X-Forwarded-For"];
            Assert.AreEqual(xForwardedFor, "2a01:e35:aaa4:6860:a5e7:5ba9:965e:cc93");
        }
    }
}
