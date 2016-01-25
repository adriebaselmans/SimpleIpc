using System.IO;

namespace Interfaces
{
    public interface IFileTransfer
    {
        MemoryStream TransferFile(string filePath);
    }
}