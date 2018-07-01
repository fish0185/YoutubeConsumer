using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace YoutubeDownloader.Services
{
    public class S3Service : IStorageService
    {
        private readonly IAmazonS3 _amazonS3;

        public S3Service(IAmazonS3 amazonS3)
        {
            _amazonS3 = amazonS3;
        }

        public async Task Upload(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new InvalidOperationException($"File: {fileName} not exist");
            }

            try
            {
                // 1. Put object-specify only key name for the new object.
                var putRequest1 = new PutObjectRequest
                {
                    BucketName = "tube-music",
                    Key = fileName,
                    InputStream = File.OpenRead(fileName)
                };

                PutObjectResponse response1 = await _amazonS3.PutObjectAsync(putRequest1);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine(
                    "Error encountered ***. Message:'{0}' when writing an object"
                    , e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Unknown encountered on server. Message:'{0}' when writing an object"
                    , e.Message);
            }
        }

        public void DeleteAll(string fileName)
        {
            var allowedDeleteExtension = new [] { "srt", "mp3", "mp4", "webm" };
            foreach (var file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory))
            {
                var targetFileName = Path.GetFileName(file);
                if (targetFileName.StartsWith(fileName) && allowedDeleteExtension.Contains(targetFileName.Split(".").LastOrDefault()))
                {
                    File.Delete(file);
                }
            } 
        }
    }
}
