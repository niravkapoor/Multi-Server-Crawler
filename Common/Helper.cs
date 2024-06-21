using System;
namespace Crawler.Common
{
	public static class Helper
    {
		public static int GetHash(string data)
		{
			return data.GetHashCode();
		}
	}
}

