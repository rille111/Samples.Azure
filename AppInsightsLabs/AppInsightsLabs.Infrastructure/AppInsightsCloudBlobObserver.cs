using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AppInsightsLabs.Infrastructure
{
    public class AppInsightsCloudBlobObserver
    {
        private readonly AppInsightsCloudBlobReader _blobReader;

        public ObservableCollection<BlobInfo> BlobInfos { get; set; }
        private List<BlobInfo> _blobInfos = new List<BlobInfo>();

        public delegate void FooDelegate<in T>(T value);
        public event FooDelegate<IEnumerable<BlobInfo>> BlobInfoAdded;


        public AppInsightsCloudBlobObserver(AppInsightsCloudBlobReader blobReader)
        {

        }

        public void AddName(string name)
        {
            var blobInfo = new BlobInfo() {Name = name};
            BlobInfoAdded?.Invoke(new [] { blobInfo });
        }
    }
}