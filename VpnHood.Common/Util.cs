﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace VpnHood.Common
{
    public static class Util
    {
        public static bool TryParseIpEndPoint(string value, out IPEndPoint ipEndPoint)
        {
            ipEndPoint = null;
            var addr = value.Split(':');
            if (addr.Length != 2) return false;
            if (!IPAddress.TryParse(addr[0], out var ipAddress)) return false;
            if (!int.TryParse(addr[1], out var port)) return false;
            ipEndPoint = new IPEndPoint(ipAddress, port);
            return true;
        }

        public static IPEndPoint ParseIpEndPoint(string value)
        {
            if (!TryParseIpEndPoint(value, out var ipEndPoint))
                throw new ArgumentException($"Could not parse {value} to an IpEndPoint");
            return ipEndPoint;
        }

        public static IPAddress GetLocalIpAddress()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 0);
            var endPoint = (IPEndPoint)socket.LocalEndPoint;
            return endPoint.Address;
        }

        public static bool IsSocketClosedException(Exception ex)
        {
            return ex is ObjectDisposedException || ex is IOException || ex is SocketException;
        }

        public static void TcpClientConnectWithTimeout(TcpClient tcpClient, string host, int port, int timeout)
        {
            var task = tcpClient.ConnectAsync(host, port);
            Task.WaitAny(new[] { task }, timeout);
            if (!tcpClient.Connected)
                tcpClient.Close();
        }

        public static IPEndPoint GetFreeEndPoint(IPAddress ipAddress, int defaultPort = 0)
        {
            try
            {
                // check recommended port
                var listener = new TcpListener(ipAddress, defaultPort);
                listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                return new IPEndPoint(ipAddress, port);
            }
            catch when (defaultPort != 0)
            {
                // try any port
                var listener = new TcpListener(ipAddress, 0);
                listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                return new IPEndPoint(ipAddress, port);
            }
        }

        public static void DirectoryCopy(string sourcePath, string destinationPath, bool recursive)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourcePath);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourcePath);
            }

            var dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destinationPath);

            // Get the files in the directory and copy them to the new location.
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var tempPath = Path.Combine(destinationPath, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (recursive)
            {
                foreach (var subdir in dirs)
                {
                    var tempPath = Path.Combine(destinationPath, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, recursive);
                }
            }
        }

        public static async Task TcpClient_ConnectAsync(TcpClient tcpClient, IPAddress address, int port, int timeout, CancellationToken cancellationToken)
        {
            if (tcpClient == null)
                throw new ArgumentNullException(nameof(tcpClient));

            using var _ = cancellationToken.Register(() => tcpClient.Close());
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var connectTask = tcpClient.ConnectAsync(address, port);
                var timeoutTask = Task.Delay(timeout);
                await Task.WhenAny(connectTask, timeoutTask);

                if (!connectTask.IsCompleted)
                    throw new TimeoutException();

                if (connectTask.IsFaulted)
                    throw connectTask.Exception.InnerException;
            }
            catch
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }
    }
}
