using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;

class Program
{
    static string sharedFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");
    static int port = 49152;

    static string bucketName = "ernzcnltn";
    static RegionEndpoint bucketRegion = RegionEndpoint.EUNorth1;
    static IAmazonS3 s3Client;

    static void Main()
    {
        if (!Directory.Exists(sharedFolderPath))
        {
            Directory.CreateDirectory(sharedFolderPath);
        }

        s3Client = new AmazonS3Client(bucketRegion);

        try
        {
            IPAddress localIPAddress = GetLocalIPAddress();
            TcpListener listener = new TcpListener(localIPAddress, port);
            listener.Start();
            Console.WriteLine($"Server launched on {localIPAddress}:{port}");

            while (true)
            {
                Console.WriteLine("Waiting for connection...");

                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected");

                Task.Run(() =>
                {
                    HandleClient(client);
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        }
    }

    static IPAddress GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    static void HandleClient(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);

            string clientMessage = reader.ReadLine();
            Console.WriteLine($"Command from client: {clientMessage}");

            string[] commandParts = clientMessage.Split(' ');
            string command = commandParts[0].ToUpper();

            switch (command)
            {
                case "LIST":
                    SendFileList(writer);
                    break;
                case "UPLOAD":
                    string fileName = commandParts[1];
                    ReceiveFile(fileName, stream);
                    break;
                case "DOWNLOAD":
                    string requestedFile = commandParts[1];
                    SendFile(requestedFile, stream);
                    break;
                default:
                    Console.WriteLine("Invalid command from client");
                    break;
            }

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        }
    }

    static void SendFileList(StreamWriter writer)
    {
        try
        {
            var directory = new DirectoryInfo(sharedFolderPath);
            foreach (var file in directory.GetFiles())
            {
                string fileInfo = $"{file.Name}|{file.LastWriteTime}";
                writer.WriteLine(fileInfo);
            }
            writer.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending file list: {ex.Message}");
        }
    }

    static async void ReceiveFile(string fileName, NetworkStream stream)
    {
        try
        {
            string filePath = Path.Combine(sharedFolderPath, fileName);
            byte[] buffer = new byte[1024];
            int bytesRead;

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, bytesRead);
                }
            }

            Console.WriteLine($"File {fileName} received and saved");

            var fileTransferUtility = new TransferUtility(s3Client);
            await fileTransferUtility.UploadAsync(filePath, bucketName);
            Console.WriteLine($"File {fileName} uploaded to S3 bucket {bucketName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving file: {ex.Message}");
        }
    }

    static async void SendFile(string fileName, NetworkStream stream)
    {
        try
        {
            string filePath = Path.Combine(sharedFolderPath, fileName);

            if (File.Exists(filePath))
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                stream.Write(fileBytes, 0, fileBytes.Length);
                stream.Flush();
                Console.WriteLine($"File {fileName} sent to client");
            }
            else
            {
                Console.WriteLine($"File {fileName} not found on server");

                var fileTransferUtility = new TransferUtility(s3Client);
                string downloadFilePath = Path.Combine(sharedFolderPath, fileName);
                await fileTransferUtility.DownloadAsync(downloadFilePath, bucketName, fileName);
                Console.WriteLine($"File {fileName} downloaded from S3 bucket {bucketName}");

                byte[] fileBytes = File.ReadAllBytes(downloadFilePath);
                stream.Write(fileBytes, 0, fileBytes.Length);
                stream.Flush();
                Console.WriteLine($"File {fileName} sent to client");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending file: {ex.Message}");
        }
    }
}