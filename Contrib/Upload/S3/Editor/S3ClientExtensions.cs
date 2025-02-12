using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace org.BasisVr.Contrib.Upload.S3
{
    public static class S3ClientExtensions
    {
        /// <summary>
        /// Puts a file in the bucket. If there is already a file with this name in the bucket, it will be replaced.
        /// </summary>
        /// <param name="s3Client"></param>
        /// <param name="bucketName">Bucket to put the file into</param>
        /// <param name="filePath">Path to the file to upload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ETag for the file. In a general purpose bucket this should be the MD5 hash of the uploaded file you can compare your local file against</returns>
        public static async Task<string> PutObjectAsync(this IAmazonS3 s3Client, string bucketName, string filePath, CancellationToken cancellationToken)
        {
            var request = new PutObjectRequest
            {
                FilePath = filePath,
                BucketName = bucketName,
                DisablePayloadSigning = true
            };

            var response = await s3Client.PutObjectAsync(request, cancellationToken);
            return response.ETag;
        }
    }
}
