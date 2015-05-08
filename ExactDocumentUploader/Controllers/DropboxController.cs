using DropNet.Models;
using ExactDocumentUploader.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExactDocumentUploader.Controllers
{
    public class DropboxController : Controller
    {
        private static readonly string DROPBOX_TOKEN = "dropboxToken";
        public static readonly string DROPBOX_ACCESS_TOKEN = "dropboxAccessToken";

        //
        // GET: /Dropbox/
        public ActionResult Index()
        {
            UserLogin token = Session["dropboxToken"] as UserLogin;

            Dropbox dropbox = new Dropbox(token);
            Session.Remove(DROPBOX_TOKEN); // We no longer need this one
            Session[DROPBOX_ACCESS_TOKEN] = dropbox.Token;

            int newFilesCount = dropbox.GetNewDocumentsCount();

            ViewBag.NewFilesCount = newFilesCount;

            return View();
        }

        public ActionResult AuthorizeDropbox()
        {
            Dropbox dropbox = new Dropbox();
            string url = dropbox.BuildAuthorizationUrl(Url.Action("Index", "Dropbox", null, Request.Url.Scheme));
            Session[DROPBOX_TOKEN] = dropbox.Token;

            return Redirect(url);
        }
	}
}