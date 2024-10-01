using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Service
{
    public interface IFileSystemService
    {
        IEnumerable<string> GetFiles(string path);
        Stream OpenRead(string filePath);
        long GetFileSize(string filePath);
    }
}
