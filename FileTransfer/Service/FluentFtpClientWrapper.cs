using FluentFTP;
using Microsoft.Extensions.Logging;

namespace FileTransfer.Service
{
    public class FluentFtpClientWrapper : IFtpClientWrapper
    {
        private readonly AsyncFtpClient _client;
        private readonly ILogger<FluentFtpClientWrapper> _logger;

        public FluentFtpClientWrapper(FileTransfer.Model.FtpConfig ftpConfig, ILogger<FluentFtpClientWrapper> logger)
        {
            _client = new AsyncFtpClient(ftpConfig.FtpServer, ftpConfig.FtpUsername, ftpConfig.FtpPassword, ftpConfig.FtpPort);
            _logger = logger;
        }

        public async Task ConnectAsync()
        {
            await _client.Connect();
            _logger.LogInformation("Connected to FTP server: {Host}", _client.Host);
        }

        public async Task DisconnectAsync()
        {
            await _client.Disconnect();
            _logger.LogInformation("Disconnected from FTP server: {Host}", _client.Host);
        }

        public async Task UploadFileAsync(string localPath, string remotePath, IProgress<FtpProgress> progress = null)
        {
            var fileInfo = new FileInfo(localPath);
            var totalBytes = fileInfo.Length;

            try
            {
                var status = await _client.UploadFile(localPath, remotePath, FtpRemoteExists.Overwrite, true, FtpVerify.Retry, progress);

                if (status == FtpStatus.Success)
                {
                    _logger.LogInformation("File uploaded successfully. Local: {LocalPath}, Remote: {RemotePath}", localPath, remotePath);
                }
                else
                {
                    _logger.LogError("File upload failed. Local: {LocalPath}, Remote: {RemotePath}, Status: {Status}", localPath, remotePath, status);
                }
            }
            catch (Exception ex) {
                _logger.LogError("File upload failed. Local: {LocalPath}, Remote: {RemotePath}, Exception: {Status}", localPath, remotePath, ex.Message);
            }
        }

        public async Task UploadDirectoryAsync(string localPath, string remotePath, IProgress<FtpProgress> progress = null)
        {
            //var fileInfo = new FileInfo(localPath);
            //var totalBytes = fileInfo.Length;

            var status = await _client.UploadDirectory(localPath, remotePath, FtpFolderSyncMode.Update);

            foreach (var item in status) {
                if (item.IsSuccess)
                {
                    _logger.LogInformation("File uploaded successfully. Local: {LocalPath}, Remote: {RemotePath}", item.LocalPath, item.RemotePath);
                }
                else
                {
                    _logger.LogError("File upload failed. Local: {LocalPath}, Remote: {RemotePath}, Exception: {Status}", item.LocalPath, item.RemotePath, item.Exception);
                    //throw new Exception($"File upload failed with status: {status}");
                }
            }
        }

        public async Task CreateDirectoryAsync(string remotePath)
        {
            await _client.CreateDirectory(remotePath);
            _logger.LogInformation("Directory created: {RemotePath}", remotePath);
        }

        public async Task<bool> DirectoryExistsAsync(string remotePath)
        {
            return await _client.DirectoryExists(remotePath);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task UploadFileAsync(string localPath, string remotePath, Action<object> value)
        {
            var fileInfo = new FileInfo(localPath);
            var totalBytes = fileInfo.Length;

            try
            {
                var status = await _client.UploadFile(localPath, remotePath, FtpRemoteExists.Overwrite, true, FtpVerify.Retry, null);

                if (status == FtpStatus.Success)
                {
                    _logger.LogInformation("File uploaded successfully. Local: {LocalPath}, Remote: {RemotePath}", localPath, remotePath);
                }
                else
                {
                    _logger.LogError("File upload failed. Local: {LocalPath}, Remote: {RemotePath}, Status: {Status}", localPath, remotePath, status);
                    throw new Exception($"File upload failed with status: {status}");
                }
            }
            catch(Exception e)
            {
                _logger.LogError("File upload failed. Local: {LocalPath}, Remote: {RemotePath}, Status: {Status} , Exception: {ex}", localPath, remotePath, FtpStatus.Failed, e.Message);
            }
            //throw new NotImplementedException();
        }
    }
}
