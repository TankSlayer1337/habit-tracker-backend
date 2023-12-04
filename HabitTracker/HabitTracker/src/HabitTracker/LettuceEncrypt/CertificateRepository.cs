using LettuceEncrypt;
using System.Security.Cryptography.X509Certificates;

namespace HabitTracker.LettuceEncrypt
{
    public class CertificateRepository : ICertificateRepository
    {
        public async Task SaveAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            var bucketClient = new Bucket();
            await bucketClient.PutCertificateAsync(certificate, cancellationToken);
        }
    }
}
