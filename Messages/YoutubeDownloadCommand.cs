using System;
using System.Collections.Generic;
using System.Text;
using JustSaying.Models;

namespace YoutubeDownloader.Messages
{
    public class YoutubeDownloadCommand : Message
    {
        public string Url { get; set; }
    }
}
