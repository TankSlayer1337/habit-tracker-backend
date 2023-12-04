using Amazon.S3;
using LettuceEncrypt;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace HabitTracker.LettuceEncrypt
{
    public class CertificateSource : ICertificateSource
    {
        public async Task<IEnumerable<X509Certificate2>> GetCertificatesAsync(CancellationToken cancellationToken)
        {
            var bucket = new Bucket();
            try
            {
                using var response = await bucket.GetCertificateAsync(cancellationToken);
                var bytes = StreamConverter.ToByteArray(response.ResponseStream);
                var certificate = new X509Certificate2(bytes);
                return new[] { certificate };
            }
            catch (AmazonS3Exception e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return Enumerable.Empty<X509Certificate2>();
                }
                throw;
            }
        }
    }
}
