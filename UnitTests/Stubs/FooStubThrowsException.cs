using System;

namespace UnitTests.Stubs
{
    public class FooStubThrowsException : FooStub
    {
        public override void Foo()
        {
            throw new DivideByZeroException();
        }
    }
}