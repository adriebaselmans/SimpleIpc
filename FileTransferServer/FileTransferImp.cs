using System;
using Interfaces;
using System.IO;
using Ipc;

namespace FileTransferServer
{
    public class FileTransferImp : IFileTransfer, IDisposable
    {
        private MemoryStream _memoryStream;
        
        public MemoryStream TransferFile(string filePath)
        {
            CleanUp();
            _memoryStream = new MemoryStream();
            using (var fileStream = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[Constants.BulkBufferSizeInBytes];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    _memoryStream.Write(buffer, 0, bytesRead);
                }
                _memoryStream.Position = 0;
            }

            return _memoryStream;
        }

        private void CleanUp()
        {
            if (_memoryStream != null)
            {
                _memoryStream.Dispose();
                _memoryStream = null;
            }
        }

        public void Dispose()
        {
            CleanUp();
        }
    }
}