namespace MetaFrm.Service
{
    internal class ServiceInfo
    {
        public HttpClient HttpClient { get; set; }
        public bool IsBusy { get; set; }
        public void End()
        {
            this.IsBusy = false;
        }

        public ServiceInfo(HttpClient service)
        {
            this.HttpClient = service;
            this.IsBusy = false;
        }
    }
}