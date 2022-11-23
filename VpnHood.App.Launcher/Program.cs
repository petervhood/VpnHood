﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace VpnHood.App.Launcher;

internal class Program
{
    private static readonly ILogger Logger = NullLogger.Instance;

    private static void Updater2_InstallerScriptExecuted(object? sender, EventArgs e)
    {
        Console.WriteLine("Finish");
    }

    private static int Main(string[] args)
    {
        // set sessionName from -launcher:sessionName:
        var sessionName = FindSessionName(args);
        if (!string.IsNullOrEmpty(sessionName))
            _ = new Mutex(true, sessionName, out _);

        // updateAndLaunch mode
        if (args.Length > 0 && args[0] == "update")
            return Update(args);

        // test mode
        if (args.Length > 0 && args[0] == "test")
        {
            Thread.Sleep(30000);
            return 0;
        }

        string appFolder = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) ??
                           throw new Exception($"Could not find {nameof(appFolder)}!");
        using Updater updater = new(appFolder, new UpdaterOptions {Logger = new SimpleLogger()});
        var res = updater.Start();
        return res;
    }

    private static string? FindSessionName(string[] args)
    {
        // get launcher sessionName
        var key = "-launcher:sessionName:";
        var sessionArg = args.FirstOrDefault(x => x.IndexOf(key, StringComparison.OrdinalIgnoreCase) == 0);

        // get test sessionName
        if (string.IsNullOrEmpty(sessionArg))
        {
            key = "-sessionName:";
            sessionArg = args.FirstOrDefault(x => x.IndexOf(key, StringComparison.OrdinalIgnoreCase) == 0);
        }

        return sessionArg?[key.Length..];
    }

    /*
     * update zipFile destinationFolder dotnetArgs"
     */
    private static int Update(string[] args)
    {
        return Update(args[1], args[2], args[3..]);
    }

    public static int Update(string zipFile, string destination, string[] dotnetArgs)
    {
        Logger.LogInformation("Preparing for extraction...");
        Thread.Sleep(3000);

        // unzip
        try
        {
            Logger.LogInformation($"Extracting '{zipFile}' to '{destination}'...");
            ZipFile.ExtractToDirectory(zipFile, destination, true);
            if (File.Exists(zipFile)) File.Delete(zipFile);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not extract!");
        }

        // launch updated app
        if (dotnetArgs.Length > 0 && !dotnetArgs.Contains("-launcher:noLaunchAfterUpdate"))
        {
            // create processStartInfo
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = destination
            };

            foreach (var arg in dotnetArgs)
                processStartInfo.ArgumentList.Add(arg);

            // Start process
            Process.Start(processStartInfo);
        }

        return 0;
    }
}