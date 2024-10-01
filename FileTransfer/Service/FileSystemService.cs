using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Service
{
    public class FileSystemService : IFileSystemService
    {
        public IEnumerable<string> GetFiles(string path) => Directory.GetFiles(path);
        public Stream OpenRead(string filePath) => File.OpenRead(filePath);
        public long GetFileSize(string filePath) => new FileInfo(filePath).Length;
    }
}
