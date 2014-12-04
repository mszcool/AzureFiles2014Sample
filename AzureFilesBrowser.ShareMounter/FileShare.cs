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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AzureFilesBrowser.ShareMounter
{
    public static class FileShare
    {
        public static void MountShare(string shareName, string driveletterAndColon, string userName, string password)
        {
            var ret = 0;
            if (!string.IsNullOrEmpty(driveletterAndColon))
            {
                UnmountShare(driveletterAndColon, false);
            }

            var res = new NETRESOURCE
            {
                dwType = ResourceType.RESOURCETYPE_DISK,
                lpLocalName = driveletterAndColon,
                lpRemoteName = shareName
            };

            ret = WNetAddConnection2(res, password, userName, 0);
            if(ret != 0)
                throw new Exception(string.Format("Unable to mount the file share due to error code {0}", ret));
        }

        public static void UnmountShare(string driveletterAndColon, bool throwException = true)
        {
            // Try to un-mount the existing share if it does exist
            int ret = WNetCancelConnection2(driveletterAndColon, 0, true);
            if (ret != 0)
            {
                var msg = string.Format("Unable to cancel file share connection, return code {0}", ret);
                if(throwException)
                    throw new Exception(msg);
                else
                    Trace.TraceError(msg);
            }
        }

        #region Native Imports

        [DllImport("Mpr.dll",
             EntryPoint = "WNetAddConnection2",
             CallingConvention = CallingConvention.Winapi)]
        private static extern int WNetAddConnection2(NETRESOURCE lpNetResource,
                                                     string lpPassword,
                                                     string lpUsername,
                                                     System.UInt32 dwFlags);

        [DllImport("Mpr.dll",
                   EntryPoint = "WNetCancelConnection2",
                   CallingConvention = CallingConvention.Winapi)]
        private static extern int WNetCancelConnection2(string lpName,
                                                        System.UInt32 dwFlags,
                                                        System.Boolean fForce);

        [StructLayout(LayoutKind.Sequential)]
        private class NETRESOURCE
        {
            public int dwScope;
            public ResourceType dwType;
            public int dwDisplayType;
            public int dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        };

        public enum ResourceType
        {
            RESOURCETYPE_DISK = 1,
        };

        #endregion
    }
}
