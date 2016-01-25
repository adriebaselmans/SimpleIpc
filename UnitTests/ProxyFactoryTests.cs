using Ipc;
using NUnit.Framework;
using UnitTests.Stubs;

namespace UnitTests
{
    [TestFixture]
    public class ProxyFactoryTests
    {
        [SetUp]
        public void Setup()
        {
            _fooStub = new FooStub();
        }

        private FooStub _fooStub;

        [Test]
        public void VerifyMethodCallsOnProxyAreForwardedToStub()
        {
            Assert.AreEqual(0, _fooStub.NumberOfFooCalls);

            var proxyDispatchStub = new ProxyDispatchStub<IFoo>(_fooStub);
            var fooProxy = ProxyFactory.CreateProxy<IFoo>(proxyDispatchStub);
            fooProxy.Foo();

            Assert.AreEqual(1, _fooStub.NumberOfFooCalls);
        }
    }
}