using System.IO;

namespace UnitTests.Stubs
{
    public interface IBulkData
    {
        Stream GetBulkyData();
        string Foo();
    }
}
