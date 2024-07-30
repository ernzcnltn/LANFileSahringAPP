# LANFileSharingApp


# Overview

This application consists of a server and client application that enables file sharing over a local network. Users can upload and download files. The application stores files both locally and in AWS S3 cloud storage. It also includes user authentication and file management functions.


# Features
- **User Authentication:** Users can login to the system and create new users.
- **File Upload and Download:** Users can upload and download their files.
- **AWS S3 Integration:** Files are stored both locally and in AWS S3.
- **File List:** View the list of uploaded files.
- **Search and Filter:** Search and filter files by name, type or date.
- **Multi-Language Support:** The application is available in Turkish and other languages.

# Requirements

- .NET Framework 4.7.2 or later

- Visual Studio 2019 or above

- AWS SDK for .NET

- SQLite Database Browser (for database management)



# Installation

- **Step 1: Download the Project**

Download the project from GitHub or the link provided.

- **Step 2: Open in Visual Studio**
  
Open Visual Studio.
Open the .sln file using the File > Open > Project/Solution menu.

- **Step 3: Install NuGet Packages**

Go to Tools > NuGet Package Manager > Manage NuGet Packages for Solution.
Install the following packages:

- AWSSDK.S3
- System.Data.SQLite

# AWS S3 Configuration

**Create an AWS S3 Account:**

Create your AWS account and create an S3 bucket.

**Configure AWS Credentials:**

Configure your AWS credentials and bucket name appropriately in the S3Helper class.

# Database Setup

**Create SQLite Database:**

The SharingDatabase class automatically creates your SQLite database. The first time you run the project, the required database tables will be created automatically.

**Tools for Database Management:**

- **SQLite Database Browser:** You can use this tool to visualize and manage your database structure. 


# Usage 

### Server Initialization

- Run the server application. The server listens on port 49152 by default.

- The server starts accepting connected clients and manages file operations.

### Client Usage

- Launch the client application, enter username, password and server IP address.

- Use the corresponding buttons to list, upload and download files.


# Using the Setup File

Double-click the setup.exe file to start the installation wizard.

Follow the steps of the installation wizard to install the application on your computer.

### Installation Steps:

- **Installation Location:** Select the directory where the application will be installed.

- **Installation:** Start the installation by clicking the Install button.

- **Finish:** After the installation is complete, click Finish.



# Troubleshooting

### Connection Problems:

Make sure the IP address is correct and the port is set appropriately.
Make sure the server is running and listening on the correct IP address.

### File Upload/Download Issues:

Make sure the file exists and is being accessed in the correct way.
Check file permissions.

### Database Issues:

Make sure the SQLite database is properly created and configured correctly.

### AWS S3 Issues:

Make sure your AWS credentials are correct and the zone settings are appropriate.


# Need to Know

### How to Configure AWS S3?

After you create your AWS S3 bucket, make sure you specify your region and bucket name correctly in the S3Helper class. You also need to configure your AWS credentials.

### How to Manage SQLite Database?

You can use tools like SQLite Database Browser to manage your database. This tool allows you to visualize your database, run queries and manage data.

### Port Info:

Port information is 49152 by default. It must be compatible between client and server.

### Debugging:

You can use console output to debug both applications. In particular, it provides information about file operations and connection errors.


