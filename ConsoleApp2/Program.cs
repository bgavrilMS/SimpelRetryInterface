namespace ConsoleApp2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }

    // This already exists, no changes
    class NetworkRequest
    {
        public string Url;
        public string Method;
        public string Body;
        public Dictionary<string, string> Headers;
    }

    // This already exists, no changes
    class NetworkResponse
    {
        public int Status;
        public string Body;
        public Dictionary<string, string> Headers;
    }

    // This already exists, no changes
    interface INetworkModule
    {
        NetworkResponse SendAsync(NetworkRequest request);
    }


    interface IHttpRetryPolicy
    {
        // If retry conditions occurs, returns true and pauses 
        // otherwise return false
        bool PauseForRetry(NetworkResponse response, int currentRetry);
    }

    class LinearRetryPolicy : IHttpRetryPolicy
    {
        public int MaxRetries;
        public int DefaultRetryDelay;
        public int[] HttpStatusCodesToRetryOn;
        
        public bool PauseForRetry(NetworkResponse response, int currentRetry)
        {
            if (currentRetry >= MaxRetries)
            {
                return false;
            }

            if (HttpStatusCodesToRetryOn.Contains(response.Status))
            {
                Thread.Sleep(DefaultRetryDelay);
                return true;
            }

            return false;
        }
    }

    // This is where all the logic goes
    class NetworkModuleWithRetries : INetworkModule
    {
        private readonly INetworkModule _networkModuleNoRetries;
        private readonly IHttpRetryPolicy _retryPolicy;

        public NetworkModuleWithRetries(
            INetworkModule networkModuleNoRetries,
            IHttpRetryPolicy retryPolicy)
        {
            _networkModuleNoRetries = networkModuleNoRetries;
            _retryPolicy = retryPolicy;
        }

        public NetworkResponse SendAsync(NetworkRequest request)
        {
            // the underlying network (custom or HttpClient) module will make the call 
            NetworkResponse response = _networkModuleNoRetries.SendAsync(request);

            int currentRetry = 0;
            while (_retryPolicy.PauseForRetry(response, currentRetry))
            {
                response = _networkModuleNoRetries.SendAsync(request);
                currentRetry++;
            }

            return response;
        }
    }

}