using System;
using System.Text.Json.Serialization;

namespace Crawler.Main.Models
{
	public class Url
	{
        [JsonPropertyName("urls")]
        public string Urls { get; set; }

        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public List<string> UrlList
        {
            get
            {
                if (this.Urls != null)
                {
                    return this.Urls.Split(",").ToList();
                }

                return null;
            }
            set
            {
                this.Urls = string.Join(",", value);
            }
        }
    }
}

