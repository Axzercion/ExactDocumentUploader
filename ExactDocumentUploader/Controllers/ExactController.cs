using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using DropNet.Models;
using ExactDocumentUploader.Helpers;
using ExactOnline.Client.Models;
using ExactOnline.Client.Sdk.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExactDocumentUploader.Controllers
{
    public class ExactController : Controller
    {
        private static WebServerClient client = Exact.CreateClient();
        private static readonly string EXACT_AUTH_STATE = "exactAuthState";

        //
        // GET: /Exact/
        public ActionResult Index()
        {
            if (string.IsNullOrEmpty(Request.QueryString["code"]))
            {
                return InitAuthentication();
            }
            else
            {
                return AuthenticationCallback();
            }
        }

        // AccessTokenManagerDelegate
        public string AccessTokenManager()
        {
            IAuthorizationState authState = Session[EXACT_AUTH_STATE] as IAuthorizationState;
            return authState.AccessToken;
        }

        private ActionResult InitAuthentication()
        {
            AuthorizationState state = new AuthorizationState();
            string uri = Request.Url.AbsoluteUri;
            uri = RemoveQueryStringFromUri(uri);
            state.Callback = new Uri(uri);

            OutgoingWebResponse response = client.PrepareRequestUserAuthorization(state);
            return response.AsActionResultMvc5();
        }

        private ActionResult AuthenticationCallback()
        {
            Session[EXACT_AUTH_STATE] = client.ProcessUserAuthorization(this.Request);

            // Call ExactOnline SDK
            ExactOnlineClient exact = new ExactOnlineClient(ConfigurationManager.AppSettings["exactOnlineUrl"], AccessTokenManager);

            UserLogin token = Session[DropboxController.DROPBOX_ACCESS_TOKEN] as UserLogin;

            List<DocumentCategory> categories = exact.For<DocumentCategory>().Select(new string[] { "ID", "Description" }).Get();

            Dropbox dropbox = new Dropbox(token, true);
            IEnumerable<string> fileNames = dropbox.GetNewDocumentNames();
            Dictionary<Guid, string> newReferences = new Dictionary<Guid, string>();
            foreach (string fileName in fileNames)
            {
                byte[] file = dropbox.GetFileBytes(fileName);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

                Document document = new Document()
                {
                    Subject = fileNameWithoutExtension,
                    Body = fileNameWithoutExtension,
                    Type = 55,
                    DocumentDate = DateTime.Now.Date,
                    Category = categories[3].ID
                };

                bool createdDocument = exact.For<Document>().Insert(ref document);

                DocumentAttachment attachment = new DocumentAttachment()
                {
                    Document = document.ID,
                    FileName = fileName,
                    FileSize = (double)file.Length,
                    Attachment = file
                };

                bool createdAttachment = exact.For<DocumentAttachment>().Insert(ref attachment);

                newReferences.Add(document.ID, fileName);
            }

            Dictionary<Guid, string> existingReferences = dropbox.GetExactOnlineReferences();

            dropbox.UpdateExactOnlineReferences(existingReferences, newReferences);

            return View();
            // Later, if necessary:
            // bool success = client.RefreshAuthorization(auth);
        }

        #region Helper methods

        private static string RemoveQueryStringFromUri(string uri)
        {
            int index = uri.IndexOf('?');
            if (index > -1)
            {
                uri = uri.Substring(0, index);
            }
            return uri;
        }

        #endregion
    }
}