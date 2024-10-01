using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace FileTransfer.Service
{
    public class UploadManager : IUploadManager
    {
        private readonly IFtpClientWrapper _ftpClient;
        private readonly ILogger<UploadManager> _logger;
        private readonly int _maxConcurrentUploads;

        public UploadManager(IFtpClientWrapper ftpClient, ILogger<UploadManager> logger)
        {
            _ftpClient = ftpClient;
            _logger = logger;
            _maxConcurrentUploads = 100;//config.GetValue<int>("UploadSettings:MaxConcurrentUploads", Environment.ProcessorCount);
        }

        public async Task UploadUsingCompression(string localFilePath, string remotePath = "")
        {
            await _ftpClient.ConnectAsync();
            // Local directory to compress and upload
            string localDirectory = localFilePath;
            string zipFileName = $"{Path.GetFileNameWithoutExtension(localFilePath)}_{DateTime.Now.ToString("ddMMyyyy_HH_mm_ss")}.zip";
            string localZipFile = Path.Combine(Directory.GetParent(localFilePath).FullName, zipFileName);
            string remoteZipPath = remotePath + zipFileName;

            try
            {
                _logger.LogInformation("Start zip compression for directory: {LocalDirectory} into zip file: {LocalZipFile}", localDirectory, localZipFile);

                // Check if the zip file already exists
                if (File.Exists(localZipFile))
                {
                    // Log the overwrite action
                    _logger.LogWarning("The zip file already exists. Overwriting: {LocalZipFile}", localZipFile);

                    // Delete the existing zip file
                    File.Delete(localZipFile);
                }

                // Perform zip compression
                ZipFile.CreateFromDirectory(localDirectory, localZipFile, CompressionLevel.Fastest, true);

                _logger.LogInformation("Finish zip compression for directory: {LocalDirectory} into zip file: {LocalZipFile}", localDirectory, localZipFile);
                
                // Upload the file to the FTP server
                await this._ftpClient.UploadFileAsync(localZipFile, remoteZipPath, progress: null);

            }
            catch (Exception ex)
            {
                // Log the exception details
                _logger.LogError(ex, "An error occurred during the zip compression or upload process. " + ex.Message);
            }
            await _ftpClient.DisconnectAsync();
        }

        public async Task UploadDirectory(string localPath, string remotePath = "")
        {
            await _ftpClient.ConnectAsync();
            if (!await this._ftpClient.DirectoryExistsAsync(remotePath))
            {
                await this._ftpClient.CreateDirectoryAsync(remotePath);
            }

            foreach (var filePath in Directory.GetFiles(localPath))
            {
                var fileName = Path.GetFileName(filePath);
                var remoteFilePath = Path.Combine(remotePath, fileName).Replace("\\", "/");

                var fileInfo = new FileInfo(filePath);
                var totalBytes = fileInfo.Length;

                await this._ftpClient.UploadFileAsync(filePath, remoteFilePath, progress: null);
                //var status = await this._ftpClient.UploadFileAsync(filePath, remoteFilePath, FtpRemoteExists.Overwrite, true, FtpVerify.Retry, null);
            }

            foreach (var dirPath in Directory.GetDirectories(localPath))
            {
                var dirName = Path.GetFileName(dirPath);
                var remoteSubDir = Path.Combine(remotePath, dirName).Replace("\\", "/");

                await this._ftpClient.UploadDirectoryAsync(dirPath, remoteSubDir, null);
                //await UploadDirectory(dirPath, remoteSubDir, _ftpClient);
            }
            await _ftpClient.DisconnectAsync();
        }

        public async Task UploadFilesAsync(string sourcePath, string destinationPath)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Starting batch upload operation. CorrelationId: {CorrelationId}, SourcePath: {SourcePath}, DestinationPath: {DestinationPath}",
                correlationId, sourcePath, destinationPath);

            await _ftpClient.ConnectAsync();

            try
            {
                await UploadDirectoryAsync(sourcePath, destinationPath, correlationId);
                _logger.LogInformation("Batch upload operation completed successfully. CorrelationId: {CorrelationId}", correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch upload operation failed. CorrelationId: {CorrelationId}", correlationId);
                throw;
            }
            finally
            {
                await _ftpClient.DisconnectAsync();
            }
        }

        private async Task UploadDirectoryAsync(string localPath, string remotePath, string correlationId)
        {
            var uploadTasks = new ConcurrentBag<Task>();
            var semaphore = new SemaphoreSlim(_maxConcurrentUploads);

            await ProcessDirectory(localPath, remotePath, uploadTasks, semaphore, correlationId);

            //await Task.WhenAll(uploadTasks);
        }

        private async Task ProcessDirectory(string localPath, string remotePath, ConcurrentBag<Task> uploadTasks, SemaphoreSlim semaphore, string correlationId)
        {
            if (!await _ftpClient.DirectoryExistsAsync(remotePath))
            {
                await _ftpClient.CreateDirectoryAsync(remotePath);
            }

            foreach (var filePath in Directory.GetFiles(localPath))
            {
                var fileName = Path.GetFileName(filePath);
                var remoteFilePath = Path.Combine(remotePath, fileName).Replace("\\", "/");

                //await semaphore.WaitAsync();

                //uploadTasks.Add(Task.Run(async () =>
                //{
                //    try
                //    {
                        await UploadFileAsync(filePath, remoteFilePath, correlationId);
                //    }
                //    finally
                //    {
                //        semaphore.Release();
                //    }
                //}));
            }

            foreach (var dirPath in Directory.GetDirectories(localPath))
            {
                var dirName = Path.GetFileName(dirPath);
                var remoteSubDir = Path.Combine(remotePath, dirName).Replace("\\", "/");
                await ProcessDirectory(dirPath, remoteSubDir, uploadTasks, semaphore, correlationId);
            }
        }

        private async Task UploadFileAsync(string localPath, string remotePath, string correlationId)
        {
            var fileId = Guid.NewGuid().ToString();
            _logger.LogInformation("Starting file upload. CorrelationId: {CorrelationId}, FileId: {FileId}, LocalPath: {LocalPath}, RemotePath: {RemotePath}",
                correlationId, fileId, localPath, remotePath);

            try
            {
                await _ftpClient.UploadFileAsync(localPath, remotePath, progress =>
                {
                    dynamic dynamicObj = progress;
                    _logger.LogInformation("Upload progress: {ProgressPercentage:F2}% ({BytesTransferred}/{TotalBytes} bytes). CorrelationId: {CorrelationId}, FileId: {FileId}", (string)dynamicObj.ProgressPercentage, (string)dynamicObj.BytesTransferred, (string)dynamicObj.TotalBytes, correlationId, fileId);
                });

                _logger.LogInformation("File upload completed successfully. CorrelationId: {CorrelationId}, FileId: {FileId}", correlationId, fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File upload failed. CorrelationId: {CorrelationId}, FileId: {FileId}, LocalPath: {LocalPath}", correlationId, fileId, localPath);
            }
        }

    }
}