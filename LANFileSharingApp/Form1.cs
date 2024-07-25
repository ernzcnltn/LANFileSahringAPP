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


namespace LANFileSharingApp
{
    
    public partial class Form1 : Form
    {
        private S3Helper s3Helper;

        ResourceManager rm = new ResourceManager("LANFileSharingApp.Resources.Strings", typeof(Form1).Assembly);

        private string serverIPAddress;
        private int port = 49152;
        private bool isLoggedIn = false;

        public Form1()
        {
            InitializeComponent();
            
            string region = "eu-north-1"; 
            s3Helper = new S3Helper(region);


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

        private void btnLogin_Click(object sender, EventArgs e)
        {
            serverIPAddress = txtServerIP.Text;

            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (SharingDatabase.ValidateUser(username, password))
            {
                MessageBox.Show("Login successful!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                isLoggedIn = true;

                lstFiles.Enabled = true;
                btnUpload.Enabled = true;
                btnDownload.Enabled = true;
                btnRefresh.Enabled = true;
                dtpDate.Enabled=true;
                cmbFileType.Enabled=true;
                txtSearch.Enabled=true;
                
                LoadFileList();
            }
            else
            {
                MessageBox.Show("Username or password is incorrect!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadFileList()
        {
            if (!isLoggedIn) 
            {               
                return;
            }

            try
            {
                TcpClient client = new TcpClient(serverIPAddress, port);
                NetworkStream stream = client.GetStream();

                byte[] listCommand = Encoding.ASCII.GetBytes("LIST\n");
                stream.Write(listCommand, 0, listCommand.Length);

                StreamReader reader = new StreamReader(stream);
                string fileList = reader.ReadToEnd();
                lstFiles.Items.Clear();
                DateTime selectedDate = dtpDate.Value.Date;
                string selectedFileType = cmbFileType.SelectedItem.ToString();
                string searchTerm = txtSearch.Text.Trim().ToLower();

                foreach (var file in fileList.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] fileInfo = file.Split('|');
                    string fileName = fileInfo[0];
                    DateTime fileDate = DateTime.Parse(fileInfo[1]);

                    
                    if ((selectedFileType == "All" || fileName.EndsWith(selectedFileType)) &&
                        fileDate.Date == selectedDate &&
                        fileName.ToLower().Contains(searchTerm))
                    {
                        lstFiles.Items.Add($"{fileName} ");
                    }
                }

                reader.Close();
                stream.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the file list: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string sourceFilePath = openFileDialog1.FileName;

                try
                {
                    
                    s3Helper.UploadFileAsync(sourceFilePath).Wait();

                    MessageBox.Show("File uploaded successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    
                    lstFiles.Items.Add(Path.GetFileName(sourceFilePath));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred while uploading the file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItem != null)
            {
                string fileName = lstFiles.SelectedItem.ToString();

                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    string destinationFilePath = Path.Combine(folderBrowserDialog1.SelectedPath, fileName);

                    try
                    {
                        TcpClient client = new TcpClient(serverIPAddress, port);
                        NetworkStream stream = client.GetStream();

                        byte[] downloadCommand = Encoding.ASCII.GetBytes("DOWNLOAD " + fileName + "\n");
                        stream.Write(downloadCommand, 0, downloadCommand.Length);

                        using (FileStream fs = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
                        {
                            byte[] bytes = new byte[1024];
                            int i;
                            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                fs.Write(bytes, 0, i);
                            }
                        }

                        stream.Close();
                        client.Close();

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

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                
                UploadFile(file);
            }
        }

        private void UploadFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            try
            {
                
                TcpClient client = new TcpClient(serverIPAddress, port);
                NetworkStream stream = client.GetStream();

                byte[] uploadCommand = Encoding.ASCII.GetBytes("UPLOAD " + fileName + "\n");
                stream.Write(uploadCommand, 0, uploadCommand.Length);

                byte[] fileBytes = File.ReadAllBytes(filePath);
                stream.Write(fileBytes, 0, fileBytes.Length);

                stream.Close();
                client.Close();

                MessageBox.Show("File uploaded successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                lstFiles.Items.Add(fileName);
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