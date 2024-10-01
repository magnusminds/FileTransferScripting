namespace FileTransfer.Service
{
    public interface IUploadManager
    {
        Task UploadFilesAsync(string sourcePath, string destinationPath);
        Task UploadUsingCompression(string localFilePath, string remotePath);
        Task UploadDirectory(string localPath, string remotePath);
    }
}
