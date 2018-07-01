using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeDownloader.Services
{
    public interface IStorageService
    {
        Task Upload(string fileName);

        void DeleteAll(string fileName);
    }
}
