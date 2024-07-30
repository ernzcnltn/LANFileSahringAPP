using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using static System.Data.Entity.Infrastructure.Design.Executor;
using System.Globalization;
using System.Resources;
using System.Threading;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon;
using System.Diagnostics;
using System.Threading.Tasks;


namespace LANFileSharingApp
{

    public partial class Form1 : Form
    {
        private S3Helper s3Helper;

        ResourceManager rm = new ResourceManager("LANFileSharingApp.Resources.Strings", typeof(Form1).Assembly);

        private string serverIPAddress;
        private int port;
        private bool isLoggedIn = false;

        public Form1()
        {
            InitializeComponent();

            string region = "eu-north-1";
            string bucketName = "ernzcnltn";
            s3Helper = new S3Helper(region, bucketName);
            port = 49152;

            SetLanguage("tr");
            lstFiles.Enabled = false;
            btnUpload.Enabled = false;
            btnDownload.Enabled = false;
            btnRefresh.Enabled = false;
            dtpDate.Enabled = false;
            cmbFileType.Enabled = false;
            txtSearch.Enabled = false;


        }




        private void SetLanguage(string culture)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

            btnLogin.Text = rm.GetString("Login");
            lblUsername.Text = rm.GetString("Username");
            lblPassword.Text = rm.GetString("Password");
            btnUpload.Text = rm.GetString("Upload");
            btnDownload.Text = rm.GetString("Download");
            btnRefresh.Text = rm.GetString("Refresh");
            cmbFileType.Text = rm.GetString("FileType");
            lblServerIP.Text = rm.GetString("ServerIP");
            btnStartServer.Text = rm.GetString("StartServer");
            lblSearchFile.Text = rm.GetString("SearchFile");

        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            serverIPAddress = txtServerIP.Text;



            string username = txtUsername.Text;
            string password = txtPassword.Text;

            // Sunucunun gerçekten çalışıp çalışmadığını ve portun geçerli olup olmadığını doğrulama
            if (await IsServerAvailableAsync(serverIPAddress, port))
            {
                if (SharingDatabase.ValidateUser(username, password))
                {
                    MessageBox.Show("Login successful!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    isLoggedIn = true;

                    lstFiles.Enabled = true;
                    btnUpload.Enabled = true;
                    btnDownload.Enabled = true;
                    btnRefresh.Enabled = true;
                    dtpDate.Enabled = true;
                    cmbFileType.Enabled = true;
                    txtSearch.Enabled = true;

                    await s3Helper.EnsureUserFolderExistsAsync(username);

                    LoadFileList();
                }
                else
                {
                    MessageBox.Show("Username or password is incorrect!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Server is not available. Check port.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<bool> IsServerAvailableAsync(string ipAddress, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(ipAddress, port);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private async void LoadFileList()
        {
            if (!isLoggedIn)
            {
                return;
            }

            try
            {

                string userPrefix = txtUsername.Text; 
                var fileList = await s3Helper.ListFilesAsync(userPrefix);

                lstFiles.Items.Clear();
                DateTime selectedDate = dtpDate.Value.Date;
                string selectedFileType = cmbFileType.SelectedItem.ToString();
                string searchTerm = txtSearch.Text.Trim().ToLower();

                foreach (var file in fileList)
                {
                    string fileName = file.Key.Replace($"{userPrefix}/", ""); 
                    DateTime fileDate = file.LastModified;

                    if ((selectedFileType == "All" || fileName.EndsWith(selectedFileType)) &&
                        fileDate.Date == selectedDate &&
                        fileName.ToLower().Contains(searchTerm))
                    {
                        lstFiles.Items.Add($"{fileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the file list: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnUpload_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string sourceFilePath = openFileDialog1.FileName;
                string userPrefix = txtUsername.Text; 

                try
                {
                    await s3Helper.UploadFileAsync(sourceFilePath, userPrefix);

                    string fileName = Path.GetFileName(sourceFilePath);
                    string localFilePath = Path.Combine(@"Uploads", fileName);
                    File.Copy(sourceFilePath, localFilePath, true);

                    MessageBox.Show("File uploaded successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    lstFiles.Items.Add(fileName);

                    await LogUploadToServerAsync(fileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred while uploading the file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task LogUploadToServerAsync(string fileName)
        {
            try
            {
                using (TcpClient client = new TcpClient(serverIPAddress, port))
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            string logMessage = $"UPLOAD {fileName}\n";
                            await writer.WriteAsync(logMessage);
                            await writer.FlushAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while logging the upload to the server: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItem != null)
            {
                string fileName = lstFiles.SelectedItem.ToString();
                string userPrefix = txtUsername.Text; 
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    string destinationFilePath = Path.Combine(folderBrowserDialog1.SelectedPath, fileName);

                    try
                    {
                        await s3Helper.DownloadFileAsync(fileName, destinationFilePath, userPrefix);

                       
                        await LogDownloadToServerAsync(fileName);

                        MessageBox.Show("File downloaded successfully!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("There was an error downloading the file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a file for download", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task LogDownloadToServerAsync(string fileName)
        {
            try
            {
                using (TcpClient client = new TcpClient(serverIPAddress, port))
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            string logMessage = $"DOWNLOAD {fileName}\n";
                            await writer.WriteAsync(logMessage);
                            await writer.FlushAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while logging the download to the server: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            SharingDatabase.InitializeDatabase();

            if (!SharingDatabase.ValidateUser("admin", "password"))
            {
                SharingDatabase.AddUser("admin", "password");

            }
            cmbFileType.Items.Add("All");
            cmbFileType.Items.Add(".txt");
            cmbFileType.Items.Add(".jpg");
            cmbFileType.Items.Add(".pdf");
            cmbFileType.Items.Add(".xls");
            cmbFileType.Items.Add(".xlsx");
            cmbFileType.Items.Add(".ppt");
            cmbFileType.Items.Add(".pptx");
            cmbFileType.Items.Add(".docx");
            cmbFileType.Items.Add(".doc");
            cmbFileType.Items.Add(".png");
            cmbFileType.Items.Add(".jpeg");
            cmbFileType.Items.Add(".ico");
            cmbFileType.Items.Add(".mp4");
            cmbFileType.SelectedIndex = 0;

            comboBoxLanguage.Items.Add("English");
            comboBoxLanguage.Items.Add("Turkish");
            comboBoxLanguage.SelectedIndex = 0;
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadFileList();
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private async void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                await UploadFileAsync(file);
            }
        }

        private async Task UploadFileAsync(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string userPrefix = txtUsername.Text; 

            try
            {
        
                await s3Helper.UploadFileAsync(filePath, userPrefix);

               
                string localFilePath = Path.Combine(@"Uploads", fileName);
                File.Copy(filePath, localFilePath, true);

           
                lstFiles.Items.Add(fileName);

                MessageBox.Show("File uploaded successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while uploading the file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadFileList();
        }

        private void cmbFileType_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadFileList();
        }

        private void dtpDate_ValueChanged(object sender, EventArgs e)
        {
            LoadFileList();
        }

        private void comboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxLanguage.SelectedItem.ToString() == "English")
            {
                SetLanguage("en");
            }
            else if (comboBoxLanguage.SelectedItem.ToString() == "Turkish")
            {
                SetLanguage("tr");
            }
        }

        private void txtServerIP_TextChanged(object sender, EventArgs e)
        {

        }



        private void btnStartServer_Click(object sender, EventArgs e)
        {
            string serverAppPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LANFileSharigServer.exe");

            try
            {
                Process.Start(serverAppPath);
                MessageBox.Show("Server started successfully!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while starting the server: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}