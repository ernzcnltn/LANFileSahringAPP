using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace LANFileSharingApp
{
    public partial class Form1 : Form
    {
        private string serverIPAddress = "172.20.26.115"; // Server IP 
        private int port = 49152;

        public Form1()
        {
            InitializeComponent();

            lstFiles.Enabled = false;
            btnUpload.Enabled = false;
            btnDownload.Enabled = false;
            btnRefresh.Enabled = false;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (SharingDatabase.ValidateUser(username, password))
            {
                MessageBox.Show("Login successful!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                lstFiles.Enabled = true;
                btnUpload.Enabled = true;
                btnDownload.Enabled = true;
                btnRefresh.Enabled = true;
                LoadFileList();
            }
            else
            {
                MessageBox.Show("Username or password is incorrect!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadFileList()
        {
            try
            {
                TcpClient client = new TcpClient(serverIPAddress, port);
                NetworkStream stream = client.GetStream();

                byte[] listCommand = Encoding.ASCII.GetBytes("LIST\n");
                stream.Write(listCommand, 0, listCommand.Length);

                StreamReader reader = new StreamReader(stream);
                string fileList = reader.ReadToEnd();
                lstFiles.Items.Clear();
                foreach (var file in fileList.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    lstFiles.Items.Add(file);
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
                string fileName = Path.GetFileName(sourceFilePath);

                try
                {
                    TcpClient client = new TcpClient(serverIPAddress, port);
                    NetworkStream stream = client.GetStream();

                    byte[] uploadCommand = Encoding.ASCII.GetBytes("UPLOAD " + fileName + "\n");
                    stream.Write(uploadCommand, 0, uploadCommand.Length);

                    byte[] fileBytes = File.ReadAllBytes(sourceFilePath);
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
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItem != null)
            {
                string fileName = lstFiles.SelectedItem.ToString();


                Console.WriteLine("File name: " + fileName);


                fileName = new string(fileName.Where(c => !char.IsControl(c)).ToArray());


                Console.WriteLine("Sanitized file name: " + fileName);

                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    try
                    {

                        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                        {
                            throw new ArgumentException("The file name contains invalid characters.");
                        }

                        string destinationPath = folderBrowserDialog1.SelectedPath;


                        Console.WriteLine("Destination path: " + destinationPath);


                        if (destinationPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                        {
                            throw new ArgumentException("The destination path contains invalid characters.");
                        }

                        string destinationFilePath = Path.Combine(destinationPath, fileName);


                        Console.WriteLine("Destination file path: " + destinationFilePath);

                        TcpClient client = new TcpClient(serverIPAddress, port);
                        NetworkStream stream = client.GetStream();

                        byte[] downloadCommand = Encoding.ASCII.GetBytes("DOWNLOAD " + fileName + "\n");
                        stream.Write(downloadCommand, 0, downloadCommand.Length);

                        using (FileStream fs = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
                        {
                            byte[] bytes = new byte[1024];
                            int bytesRead;
                            while ((bytesRead = stream.Read(bytes, 0, bytes.Length)) > 0)
                            {
                                fs.Write(bytes, 0, bytesRead);
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
                MessageBox.Show("Please select a file to download", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SharingDatabase.InitializeDatabase();
           
            if (!SharingDatabase.ValidateUser("admin", "password"))
            {
                SharingDatabase.AddUser("admin", "password");

            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadFileList();
        }
    }
}