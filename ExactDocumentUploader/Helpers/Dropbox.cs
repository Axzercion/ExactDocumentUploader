using DropNet;
using DropNet.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace ExactDocumentUploader.Helpers
{
    /// <summary>
    /// Helper class for Dropbox functionality
    /// </summary>
    public class Dropbox
    {
        // Name of the file containing a CSV list of the file names and their ID's in Exact Online
        private static readonly string EXACT_ONLINE_REFERENCES_FILENAME = ".ExactOnlineReferences";

        private readonly DropNetClient _dropboxClient;

        public UserLogin Token { private set; get; }

        private readonly bool _isAccessToken;

        /// <summary>
        /// Constructor to start the Dropbox authentication process. Follow this call with BuildAuthorizationUrl and redirect the user.
        /// </summary>
        public Dropbox()
        {
            _dropboxClient = new DropNetClient(ConfigurationManager.AppSettings["dropboxApiKey"], ConfigurationManager.AppSettings["dropboxAppSecret"]);
            _dropboxClient.UseSandbox = true;

            // Obtain the authentication token
            Token = _dropboxClient.GetToken();

            _isAccessToken = false;
        }

        /// <summary>
        /// Constructor to make calls to Dropbox. After authorization the access is not yet granted to the user. Granting is also
        /// done in this constructor.
        /// </summary>
        /// <param name="token">Authentication token or access token</param>
        /// <param name="isAccessToken">Indicate whether the token is the authentication token or the access token</param>
        public Dropbox(UserLogin token, bool isAccessToken = false)
        {
            string apiKey = ConfigurationManager.AppSettings["dropboxApiKey"], appSecret = ConfigurationManager.AppSettings["dropboxAppSecret"];
            _dropboxClient = new DropNetClient(apiKey, appSecret, token.Token, token.Secret);

            _isAccessToken = isAccessToken;

            if (!isAccessToken)
            {
                Token = _dropboxClient.GetAccessToken();
                _dropboxClient = new DropNetClient(apiKey, appSecret, Token.Token, Token.Secret);
                _isAccessToken = true;
            }

            // Force the root directory to Apps/ExactDocumentUploader
            _dropboxClient.UseSandbox = true;
        }

        /// <summary>
        /// Obtain the authorization URL to redirect the user to.
        /// </summary>
        /// <param name="callback">URL to send the user to after OAuth has been successful</param>
        /// <returns>Authorization URL</returns>
        public string BuildAuthorizationUrl(string callback)
        {
            if (_isAccessToken)
                throw new InvalidOperationException("User already authorized");

            return _dropboxClient.BuildAuthorizeUrl(Token, callback);
        }

        public Dictionary<Guid, string> GetExactOnlineReferences()
        {
            if (!_isAccessToken)
                throw new InvalidOperationException("User must be authorized");

            Dictionary<Guid, string> referenceDictionary = new Dictionary<Guid, string>();
            try
            {
                string references = Encoding.UTF8.GetString(_dropboxClient.GetFile("/" + EXACT_ONLINE_REFERENCES_FILENAME));
                using (StringReader referencesReader = new StringReader(references))
                {
                    string reference = null;
                    while ((reference = referencesReader.ReadLine()) != null)
                    {
                        string[] singleReference = reference.Split(';');
                        referenceDictionary.Add(Guid.Parse(singleReference[0]), singleReference[1]);
                    }
                }
            }
            catch (DropNet.Exceptions.DropboxRestException)
            {
                // Create file for future use
                UploadExactOnlineReferences("");
            }

            return referenceDictionary;
        }

        public void UpdateExactOnlineReferences(Dictionary<Guid, string> existingReferences, Dictionary<Guid, string> newReferences)
        {
            if (!_isAccessToken)
                throw new InvalidOperationException("User must be authorized");

            StringBuilder references = new StringBuilder();
            foreach (Guid exactOnlineID in existingReferences.Keys)
                references.AppendLine(exactOnlineID.ToString() + ";" + existingReferences[exactOnlineID]);

            foreach (Guid exactOnlineID in newReferences.Keys)
                references.AppendLine(exactOnlineID.ToString() + ";" + newReferences[exactOnlineID]);

            UploadExactOnlineReferences(references.ToString());
        }

        private void UploadExactOnlineReferences(string references)
        {
            _dropboxClient.UploadFile("/", EXACT_ONLINE_REFERENCES_FILENAME, Encoding.UTF8.GetBytes(references), true);
        }

        /// <summary>
        /// Get the raw file.
        /// </summary>
        /// <param name="fileName">Name of the file to get</param>
        /// <returns>Raw bytes of the file</returns>
        public byte[] GetFileBytes(string fileName)
        {
            if (!_isAccessToken)
                throw new InvalidOperationException("User must be authorized");

            byte[] file = _dropboxClient.GetFile("/" + fileName);
            return file;
        }

        /// <summary>
        /// Get the names of all new documents.
        /// </summary>
        /// <returns>List of filenames</returns>
        public IEnumerable<string> GetNewDocumentNames()
        {
            if (!_isAccessToken)
                throw new InvalidOperationException("User must be authorized");

            Dictionary<Guid, string> references = GetExactOnlineReferences();
            MetaData directoryListing = _dropboxClient.GetMetaData("/", null);
            // Exclude directories, deleted files, the reference file and any documents already in Exact Online
            IEnumerable<string> documentNames = directoryListing.Contents.Where(content => !content.Is_Dir && !content.Is_Deleted && content.Name != EXACT_ONLINE_REFERENCES_FILENAME && !references.ContainsValue(content.Name)).Select(file => file.Name);
            return documentNames;
        }

        public IEnumerable<string> GetDocumentNames()
        {
            if (!_isAccessToken)
                throw new InvalidOperationException("User must be authorized");

            MetaData directoryListing = _dropboxClient.GetMetaData("/", null);
            IEnumerable<string> documentNames = directoryListing.Contents.Where(content => !content.Is_Dir && !content.Is_Deleted && content.Name != EXACT_ONLINE_REFERENCES_FILENAME).Select(file => file.Name);
            return documentNames;
        }

        /// <summary>
        /// Get a count of all new documents.
        /// </summary>
        /// <returns>Count of new documents</returns>
        public int GetNewDocumentsCount()
        {
            if (!_isAccessToken)
                throw new InvalidOperationException("Dropbox must be accessed first");

            Dictionary<Guid, string> references = GetExactOnlineReferences();
            MetaData directoryListing = _dropboxClient.GetMetaData("/", null);
            int count = directoryListing.Contents.Where(content => !content.Is_Dir && !content.Is_Deleted && content.Name != EXACT_ONLINE_REFERENCES_FILENAME && !references.ContainsValue(content.Name)).Count();
            return count;
        }
    }
}