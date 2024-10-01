using FluentFTP;

namespace FileTransfer.Service
{
    public interface IFtpClientWrapper : IDisposable
    {
        Task ConnectAsync();
        Task DisconnectAsync();
        Task UploadFileAsync(string localPath, string remotePath, IProgress<FtpProgress> progress = null);
        Task CreateDirectoryAsync(string remotePath);
        Task<bool> DirectoryExistsAsync(string remotePath);
        Task UploadFileAsync(string localPath, string remotePath, Action<object> value);
        Task UploadDirectoryAsync(string localPath, string remotePath, IProgress<FtpProgress> progress = null);
    }
}
