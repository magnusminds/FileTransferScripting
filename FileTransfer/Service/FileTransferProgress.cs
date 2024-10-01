namespace FileTransfer.Service
{
    public class FileTransferProgress
    {
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public double ProgressPercentage => TotalBytes > 0 ? (double)BytesTransferred / TotalBytes * 100 : 0;
    }
}
