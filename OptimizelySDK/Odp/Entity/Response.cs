namespace OptimizelySDK.Odp.Entity
{
    public class Response
    {
        public Data Data { get; set; }

        public Error[] Errors { get; set; }

        public bool HasErrors
        {
            get
            {
                return Errors != null && Errors.Length > 0;
            }
        }
    }
}
