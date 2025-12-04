using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace MailClient // Đổi namespace theo dự án của bạn
{
    public class AccountTokenStore : IDataStore
    {
        private Account _account;

        public AccountTokenStore(Account account)
        {
            _account = account;
        }

        public Task ClearAsync()
        {
            _account.TokenJson = null;
            return Task.CompletedTask;
        }

        public Task DeleteAsync<T>(string key)
        {
            _account.TokenJson = null;
            return Task.CompletedTask;
        }

        public Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(_account.TokenJson))
                return Task.FromResult<T>(default(T));

            var value = JsonConvert.DeserializeObject<T>(_account.TokenJson);
            return Task.FromResult(value);
        }

        public Task StoreAsync<T>(string key, T value)
        {
            var json = JsonConvert.SerializeObject(value);
            _account.TokenJson = json;
            return Task.CompletedTask;
        }
    }
}