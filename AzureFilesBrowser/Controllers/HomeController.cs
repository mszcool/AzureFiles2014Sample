// 
// MIT License 
// Copyright (c) 2014 Microsoft. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person 
// obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, distribute, 
// sublicense, and/or sell copies of the Software, and to permit persons to 
// whom the Software is furnished to do so, subject to the following conditions: 
// 
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software. 
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
//
#define USEVBLIB

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AzureFilesBrowser.Models;

namespace AzureFilesBrowser.Controllers
{
    public class HomeController : Controller
    {
        private const string SHAREDRIVELETTER = "S:";

        public ActionResult Index()
        {
            ViewBag.Title = "Azure Files Test";

            return View();
        }

        [HttpPost]
        public ActionResult Index(FileShareDataModel model)
        {
            try
            {
                // First try to mount the share
#if USEVBLIB
                VBShareMounter.FileShare.MountShare
                    (
                        model.ShareName,
                        SHAREDRIVELETTER,
                        model.UserName,
                        model.Password
                    );
#else
                ShareMounter.FileShare.MountShare
                    (
                        model.ShareName,
                        SHAREDRIVELETTER,
                        model.UserName,
                        model.Password
                    );
#endif

                // All worked, initialize the appropriate member of the model
                model.FileShareContents = new List<FileShareContent>();

                // Next try to read the directories
                var dirInfo = System.IO.Directory.GetDirectories(string.Concat(SHAREDRIVELETTER, "\\"));
                foreach (var d in dirInfo)
                {
                    model.FileShareContents.Add(new FileShareContent { ContentName = d, ContentType = "Directory"});
                }
                var fileInfo = System.IO.Directory.GetFiles(string.Concat(SHAREDRIVELETTER, "\\"));
                foreach (var f in fileInfo)
                {
                    model.FileShareContents.Add(new FileShareContent {ContentName = f, ContentType = "File"});
                }

                // Then unmount the share again
#if USEVBLIB
                VBShareMounter.FileShare.UnmountShare(SHAREDRIVELETTER);
#else
                ShareMounter.FileShare.UnmountShare(SHAREDRIVELETTER);
#endif

                model.MountResult = "Mounting and unmounting the share did work, successfully!";
            }
            catch (Exception ex)
            {
                model.MountResult = 
                    string.Format("Error occured when mounting the file share: {0}<br/>{1}", ex.Message, ex);
            }

            return View(model);
        }
    }
}
