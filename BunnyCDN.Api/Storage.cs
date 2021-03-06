using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using BunnyCDN.Api.Internals;

namespace BunnyCDN.Api
{
    /// <summary>
    /// Storage class interface
    /// </summary>
    public interface StorageInterface
    {
        string Zone { get; }
        
        Task<byte[]> GetFile(string path);
        Task<StorageEntry[]> GetFolder(string path);
        Task<bool> Put(byte[] fileContent, string path);
        Task<bool> Delete(string path);
    }

    /// <summary>
    /// Storage API endpoint interface
    /// </summary>
    public class Storage : StorageInterface
    {
        /// <summary>
        /// Storagezone name set for the desired node
        /// </summary>
        /// <value>Storage zone name</value>
        public string Zone { get { return _zone; } }
        /// <summary>
        /// StorageKey, used to retrieve the required HttpClient
        /// </summary>
        private readonly StorageKey StorageKey;
        /// <summary>
        /// The internal zone string
        /// </summary>
        private readonly string _zone;

        /// <summary>
        /// Storage API interface
        /// </summary>
        /// <param name="sKey">StorageKey token-provider</param>
        /// <param name="zone">Zone name for the desired node</param>
        public Storage(StorageKey sKey, string zone)
        {
            if (sKey == null || string.IsNullOrWhiteSpace(sKey.Token))
                throw new BunnyTokenException("Invalid StorageKey provided!");

            if (string.IsNullOrWhiteSpace(zone))
                throw new BunnyZoneException();

            this.StorageKey = sKey;
            this._zone = zone;
        }

        /// <summary>
        /// Storage API interface
        /// </summary>
        /// <param name="storageToken">Token string</param>
        /// <param name="zone">Zone name for the desired node</param>
        public Storage(string storageToken, string zone)
        {
            if (string.IsNullOrWhiteSpace(storageToken))
                throw new BunnyTokenException("No Storage token provided!");

            if (string.IsNullOrWhiteSpace(zone))
                throw new BunnyZoneException();

            this.StorageKey = new StorageKey(storageToken);
            this._zone = zone;
        }

        /// <summary>
        /// Retrieve a file from the storage API
        /// </summary>
        /// <param name="path">file path (without prefixing slash)</param>
        /// <returns>The file in a byte array, throws if failed</returns>
        public async Task<byte[]> GetFile(string path)
        {
            HttpResponseMessage httpResponse = await this.StorageKey.Client.GetAsync( GetPath(path) );
            byte[] content;
            switch (httpResponse.StatusCode)
            {
                case HttpStatusCode.OK:
                    content = await httpResponse.Content.ReadAsByteArrayAsync();
                    break;
                case HttpStatusCode.Unauthorized:
                    throw new BunnyUnauthorizedException();
                case HttpStatusCode.NotFound:
                    throw new BunnyNotFoundException();
                default:
                    throw new BunnyInvalidResponseException("Unexpected/unhandled response retrieved");
            }
            return content;
        }

        /// <summary>
        /// Retrieve objects inside folder from the storage API (not-recursive)
        /// </summary>
        /// <param name="path">Path to retrieve objects from</param>
        /// <returns>StorageEntry array containing the objects</returns>
        public async Task<StorageEntry[]> GetFolder(string path)
        {
            if (!path.EndsWith("/"))
            {
                // Append slash to ensure folder retrieval.
                path += "/";
            }

            HttpResponseMessage httpResponse = await this.StorageKey.Client.GetAsync( GetPath(path) ); 
            switch (httpResponse.StatusCode)
            {
                case HttpStatusCode.OK:
                    return await JsonWrapper.Deserialize<StorageEntry[]>(httpResponse);
                case HttpStatusCode.BadRequest:
                    try {
                        ErrorMessage error = await JsonWrapper.Deserialize<ErrorMessage>(httpResponse);
                        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                            throw new BunnyBadRequestException(error.Message);
                    } catch (BunnyInvalidResponseException) {
                        throw new BunnyBadRequestException("Invalid response error provided.");
                    }

                    throw new BunnyBadRequestException("No response error provided.");
                case HttpStatusCode.Unauthorized:
                    throw new BunnyUnauthorizedException();
                case HttpStatusCode.NotFound:
                    throw new BunnyNotFoundException();
                default:
                    throw new BunnyInvalidResponseException("Unexpected/unhandled response retrieved");
            }
        }

        /// <summary>
        /// Creates/overwrites a file (and the missing path) in your storage zone.
        /// </summary>
        /// <param name="fileContent">File contents</param>
        /// <param name="path">Path to store file in zone</param>
        /// <returns>Success</returns>
        public async Task<bool> Put(byte[] fileContent, string path)
        {
            using (HttpContent httpContent = new ByteArrayContent(fileContent))
            {
                HttpResponseMessage httpResponse = await this.StorageKey.Client.PutAsync(GetPath(path), httpContent);
                switch (httpResponse.StatusCode)
                {
                    case HttpStatusCode.Created:
                        return true;
                    case HttpStatusCode.Unauthorized:
                        throw new BunnyUnauthorizedException();
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Deletes a file/directory-path from a the storage zone.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Success</returns>
        public async Task<bool> Delete(string path)
        {
            HttpResponseMessage httpResponse = await this.StorageKey.Client.DeleteAsync( GetPath(path) );
            switch (httpResponse.StatusCode)
            {
                case HttpStatusCode.OK:
                    return true;
                case HttpStatusCode.Unauthorized:
                    throw new BunnyUnauthorizedException();
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets a valid API URL string.
        /// </summary>
        /// <param name="path">Input path</param>
        /// <returns>A valid URL for API calls</returns>
        private string GetPath(string path) => $"{Variables.StorageUrl}{this.Zone}/{path}";
    }
}