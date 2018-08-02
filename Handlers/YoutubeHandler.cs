using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;
using YoutubeDownloader.Messages;
using YoutubeDownloader.Services;

namespace YoutubeDownloader.Handlers
{
    public class YoutubeHandler : IHandlerAsync<YoutubeDownloadCommand>
    {
        private readonly ILogger<YoutubeHandler> _logger;
        private readonly IStorageService _storageService;

        public YoutubeHandler(ILogger<YoutubeHandler> logger, IStorageService storageService)
        {
            _logger = logger;
            _storageService = storageService;
        }

        public async Task<bool> Handle(YoutubeDownloadCommand message)
        {
            if (string.IsNullOrWhiteSpace(message.Url))
            {
                throw new InvalidEnumArgumentException();
            }

            if (TryProcessDownloadMp3(message.Url, out var filename))
            {
               _logger.LogInformation($"Uploading {filename}.mp3 to S3.");
               await _storageService.Upload(filename + ".mp3");
               _storageService.DeleteAll(filename);
               _logger.LogInformation($"Delete files completed.");
               return true;
            }

            return false;
        }

        private bool TryProcessDownloadMp3(string url, out string outputfile)
        {
            try
            {
                _logger.LogInformation($"Start process url: {url}");
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;
                var workingDir = Directory.GetCurrentDirectory();

                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    FileName = "you-get",
                    WorkingDirectory = workingDir,
                    Arguments = $"  {url}"
                };
                var exeProcess = Process.Start(startInfo);
                var fileNameLine = "";
                while (!exeProcess.StandardOutput.EndOfStream)
                {
                    string line = exeProcess.StandardOutput.ReadLine();
                    // do something with line
                    Console.WriteLine(line);
                    if (line.StartsWith("Downloading"))
                    {
                        fileNameLine = line;
                    }
                }

                if (!string.IsNullOrWhiteSpace(fileNameLine))
                {
                    var filenameWithExtension = fileNameLine.Substring(11).TrimEnd('.').Trim();
                    var index = filenameWithExtension.LastIndexOf('.');
                    var filename = filenameWithExtension.Substring(0, index);
                     _logger.LogInformation($"Converting {filename} to mp3.");
                    var startInfo2 = new ProcessStartInfo
                    {
                        CreateNoWindow = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "ffmpeg",
                        WorkingDirectory = workingDir,
                        Arguments = $" -i \"{filenameWithExtension}\" -vn -f mp3 -ab 192k \"{filename}.mp3\""
                    };
                    using (var ffmpegProcess = Process.Start(startInfo2))
                    {
                        ffmpegProcess.WaitForExit();
                    }

                    outputfile = filename;
                    return true;
                }

                outputfile = string.Empty;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on YoutubeHandler::DownloadMp3");
                throw;
            }
        }
    }
}
