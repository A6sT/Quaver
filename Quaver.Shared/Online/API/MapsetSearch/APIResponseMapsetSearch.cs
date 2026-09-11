using System.Collections.Generic;
using Newtonsoft.Json;

namespace Quaver.Shared.Online.API.MapsetSearch
{
    public class APIResponseMapsetSearch
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("mapsets")]
        public List<DownloadableMapset> Mapsets { get; set; }
    }
}
