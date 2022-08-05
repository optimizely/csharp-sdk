namespace OptimizelySDK.Odp
{
    public interface ILruCache<T>
    {
        void Save(string key, T value);
        T Lookup(string key);
        void Reset();
    }
}