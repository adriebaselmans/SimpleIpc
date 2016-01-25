namespace UnitTests.Stubs
{
    public class FooStub : IFoo
    {
        public FooStub()
        {
            NumberOfFooCalls = 0;
            NumberOfBarCalls = 0;
        }

        public int NumberOfFooCalls { get; private set; }
        public int NumberOfBarCalls { get; private set; }

        public string ReturnValueOfBar { get; set; }

        public virtual void Foo()
        {
            NumberOfFooCalls++;
        }

        public virtual string Bar()
        {
            NumberOfBarCalls++;
            return ReturnValueOfBar;
        }
    }
}