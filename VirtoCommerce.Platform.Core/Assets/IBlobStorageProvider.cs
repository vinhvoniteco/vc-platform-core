using System.IO;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Platform.Core.Assets
{
    /// <summary>
    /// Represent abstraction for working with binary data
    /// </summary>
    public interface IBlobStorageProvider
    {
        /// <summary>
        /// SearchAsync blobs by specified criteria
        /// </summary>
        /// <param name="folderUrl"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        Task<GenericSearchResult<BlobEntry>> SearchAsync(string folderUrl, string keyword);

        /// <summary>
        /// Get blog info by url
        /// </summary>
        /// <param name="blobUrl"></param>
        /// <returns></returns>
        Task<BlobInfo> GetBlobInfoAsync(string blobUrl);

        /// <summary>
        /// Create blob folder in specified provider
        /// </summary>
        /// <param name="folder"></param>
        Task CreateFolderAsync (BlobFolder folder);

        /// <summary>
        /// Open blob for reading
        /// </summary>
        /// <param name="blobUrl">Realative or absolute blob url (tmp/blob.txt) </param>
        /// <returns></returns>
        Stream OpenRead(string blobUrl);

        /// <summary>
        /// Open blob for writing
        /// </summary>
        /// <param name="blobUrl">Realative or absolute blob url (tmp/blob.txt)</param>
        /// <returns></returns>
        Stream OpenWrite(string blobUrl);

        /// <summary>
        /// Remove secified blobs
        /// </summary>
        /// <param name="urls"></param>
        Task RemoveAsync (string[] urls);
    }
}
