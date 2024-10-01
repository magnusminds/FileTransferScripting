namespace FileTransfer.Model
{
    public class FtpConfig
    {
        public string Name { get; set; }
        public string FtpServer { get; set; }
        public string FtpUsername { get; set; }
        public string FtpPassword { get; set; }
        public int FtpPort { get; set; }
        public string FtpPath { get; set; }
    }
}
