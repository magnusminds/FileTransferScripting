using FileTransfer.Model;
using FileTransfer.Service;
using FluentFTP;
using FluentFTP.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace FileTransfer
{
    public partial class Form1 : Form
    {
        private List<string> selectedPaths { get; set; }
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Browse_Click(object sender, EventArgs e)
        {
            // Create an instance of OpenFileDialog
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // List to store both file and folder paths
                selectedPaths = new List<string>();

                // Step 2: Select folders using FolderBrowserDialog
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select a folder";

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        textBox1.Text = folderDialog.SelectedPath;
                        selectedPaths.Add(folderDialog.SelectedPath); // Add the selected folder to the list
                    }
                }

                // Show the selected files and folders
                //if (selectedPaths.Any())
                //{
                //    string allPaths = string.Join(Environment.NewLine, selectedPaths);
                //    MessageBox.Show("Selected files and folders:\n" + allPaths);
                //}
                //else
                //{
                //    MessageBox.Show("No files or folders selected.");
                //}
            }
        }

        private async void Share_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("Please select Directory");
                return;
            }

            FtpSettings ftpSettings = AppSettings.GetFtpSettings();

            await Parallel.ForEachAsync(ftpSettings.Servers, async (item, cancellationToken) =>
            {
                // create Serilog logger
                var serilogLogger = new LoggerConfiguration()
                                        .MinimumLevel.Debug()
                                        .WriteTo.File(Path.Combine("logs", $"Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt"), rollingInterval: RollingInterval.Day)
                                        .CreateLogger();

                var loggerFactory = new LoggerFactory().AddSerilog(serilogLogger);

                var uploadLogger = loggerFactory.CreateLogger<UploadManager>();
                var ftpClientLogger = loggerFactory.CreateLogger<FluentFtpClientWrapper>();

                //IFtpClientWrapper ftpClientWrapper = new FluentFtpClientWrapper("192.168.1.99", "magnusminds", "admin", ftpClientLogger);
                IFtpClientWrapper ftpClientWrapper = new FluentFtpClientWrapper(item, ftpClientLogger);
                IUploadManager uploadManager = new UploadManager(ftpClientWrapper, uploadLogger);
                Stopwatch sw = Stopwatch.StartNew();
                sw.Start();
                uploadLogger.LogInformation("File uploading started");

                if (checkBox1.Checked)
                {
                    await uploadManager.UploadUsingCompression(textBox1.Text, item.FtpPath);
                }
                else
                {
                    await uploadManager.UploadDirectory(textBox1.Text, item.FtpPath);
                }

                uploadLogger.LogInformation($"It takes " + sw.Elapsed.TotalMinutes + " minutes to upload - " + item.Name);

                sw.Stop();
            });

            MessageBox.Show("File transfer successfully.");
        } 
    }
}
