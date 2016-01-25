using System;
using System.IO;

namespace UnitTests.Stubs
{
    public class BulkDataStub : IBulkData, IDisposable
    {
        private MemoryStream _bulkData;

        public BulkDataStub(byte[] buffer)
        {
            _bulkData = new MemoryStream(buffer);
        }

        public Stream GetBulkyData()
        {
            return _bulkData;
        }

        public void Dispose()
        {
            if (_bulkData != null)
            {
                _bulkData.Dispose();
                _bulkData = null;
            }
        }

        public string Foo()
        {
            return "Bar";
        }
    }
}