using Amazon.S3;
using LettuceEncrypt.Accounts;
using Newtonsoft.Json;
using System.Net;

namespace HabitTracker.LettuceEncrypt
{
    public class AccountStore : IAccountStore
    {
        public async Task SaveAccountAsync(AccountModel account, CancellationToken cancellationToken)
        {
            var bucket = new Bucket();
            var accountSerializable = new AccountModelSerializable(account);
            await bucket.PutAccountAsync(accountSerializable, cancellationToken);
        }

        public async Task<AccountModel?> GetAccountAsync(CancellationToken cancellationToken)
        {
            var bucket = new Bucket();
            try
            {
                using var response = await bucket.GetAccountAsync(cancellationToken);
                var reader = new StreamReader(response.ResponseStream);
                var serializedAccountModel = reader.ReadToEnd();
                var accountModelSerializable = JsonConvert.DeserializeObject<AccountModelSerializable>(serializedAccountModel) ?? throw new Exception("Failed to deserialize LettuceEncrypt Account data");
                return accountModelSerializable.ToAccountModel();
            }
            catch (AmazonS3Exception e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                throw;
            }
        }
    }
}
