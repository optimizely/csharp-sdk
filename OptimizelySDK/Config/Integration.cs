namespace OptimizelySDK.Config
{
    public class Integration
    {
        public string Key { get; private set; }
        public string Host { get; private set; }
        public string PublicKey { get; private set; }

        public Integration(
            string key,
            string host,
            string publicKey
        )
        {
            Key = key;
            Host = host;
            PublicKey = publicKey;
        }

        public override string ToString()
        {
            return $"Integration{{key='{Key}', host='{Host}', publicKey='{PublicKey}'}}";
        }
    }
}