using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace FastDFS.Client
{
    public class FastDFSHelper
    {
        private static volatile object lockhelper = new object();

        static volatile FastDFSHelper _instance = null;

        /// <summary>
        /// 获取单例
        /// </summary>
        public static FastDFSHelper Current
        {
            get
            {
                if (_instance == null)
                {
                    lock (lockhelper)
                    {
                        if (_instance == null)
                        {
                            List<IPEndPoint> trackerIPs = new List<IPEndPoint>();
                            string[] tracker_ips = ConfigurationManager.AppSettings["FastDFSIPList"].Split(',');
                            foreach (string tracker_ip in tracker_ips)
                            {
                                string[] ip = tracker_ip.Split(':');
                                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip[0]), int.Parse(ip[1]));
                                trackerIPs.Add(endPoint);
                            }
                            ConnectionManager.Initialize(trackerIPs);

                            _instance = new FastDFSHelper();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// 第一次上传文件
        /// </summary>
        /// <param name="group">卷名</param>
        /// <param name="Request">HttpRequest</param>
        /// <param name="file">上传文件</param>
        /// <param name="filename">远程文件名</param>
        /// <param name="fullLenth">文件总长度</param>
        /// <returns></returns>
        public bool Upload(string group, HttpRequest Request, HttpPostedFile file, out string filename, out long endPosition, out long fullLenth)
        {
            filename = null;
            //获取远程文件系统卷
            StorageNode node = FastDFSClient.GetStorageNode(group);

            //FastDFS文件长度
            long lStartPos = 0;
            //开始读取位置
            long startPosition = 0;
            //结束读取位置
            endPosition = 0;
            fullLenth = 0;
            var contentRange = Request.Headers["Content-Range"];
            //bytes 10000-19999/1157632           
            if (!string.IsNullOrEmpty(contentRange))
            {
                contentRange = contentRange.Replace("bytes", "").Trim();
                string length = contentRange.Substring(contentRange.IndexOf("/") + 1);
                fullLenth = long.Parse(length);
                contentRange = contentRange.Substring(0, contentRange.IndexOf("/"));

                string[] ranges = contentRange.Split('-');
                startPosition = long.Parse(ranges[0]);
                endPosition = long.Parse(ranges[1]);
            }

            string[] fileExtN = file.FileName.Split('.');
            string ExtN = fileExtN.Length == 1 ? "noknow" : fileExtN[fileExtN.Length - 1];
            byte[] nbytes = new byte[endPosition - startPosition + 1];
            int result = file.InputStream.Read(nbytes, 0, nbytes.Length);
            filename = FastDFSClient.UploadAppenderFile(node, nbytes, ExtN);

            #region 作废
            /*
            //缓冲1024个字节长度
            byte[] nbytes = new byte[1024];
            int nReadSize = 0;
            nReadSize = file.InputStream.Read(nbytes, 0, nbytes.Length);
            int i = 0;
            while (nReadSize > 0)
            {
                try
                {
                    if (i == 0)//第一次上传1024字节
                    {
                        filename = FastDFSClient.UploadAppenderFile(node, nbytes, ExtN);
                    }
                    else//续传字节
                    {
                        FastDFSClient.AppendFile(group, filename, nbytes);
                    }
                    i++;
                }
                catch
                {
                    return false;
                }
            }
            */
            #endregion

            return true;
        }

        /// <summary>
        /// 一次性上传文件
        /// </summary>
        /// <param name="group"></param>
        /// <param name="nbytes"></param>
        /// <param name="ExtN"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool Upload(string group, byte[] nbytes, string ExtN, out string filename)
        {
            filename = null;
            StorageNode node = FastDFSClient.GetStorageNode(group);
            filename = FastDFSClient.UploadAppenderFile(node, nbytes, ExtN);

            return true;
        }

        /// <summary>
        /// 续传文件
        /// </summary>
        /// <returns></returns>
        public bool UploadAppend(string group, HttpRequest Request, HttpPostedFile file, string filename, out long endPosition, out long fullLenth)
        {
            //获取远程文件系统卷
            StorageNode node = FastDFSClient.GetStorageNode(group);

            //本地文件长度
            long lStartPos = 0;
            //开始读取位置
            long startPosition = 0;
            //结束读取位置
            endPosition = 0;
            fullLenth = 0;
            var contentRange = Request.Headers["Content-Range"];
            //bytes 10000-19999/1157632           
            if (!string.IsNullOrEmpty(contentRange))
            {
                contentRange = contentRange.Replace("bytes", "").Trim();
                string length = contentRange.Substring(contentRange.IndexOf("/") + 1);
                fullLenth = long.Parse(length);
                contentRange = contentRange.Substring(0, contentRange.IndexOf("/"));

                string[] ranges = contentRange.Split('-');
                startPosition = long.Parse(ranges[0]);
                endPosition = long.Parse(ranges[1]);
            }

            //判断远程的文件是否存在，如果存在则获取文件信息
            FDFSFileInfo fileInfo = null;
            try
            {
                fileInfo = FastDFSClient.GetFileInfo(node, filename);
            }
            catch { throw new Exception("远程文件不存在，卷：" + group + "；文件名：" + filename); }

            lStartPos = fileInfo.FileSize;

            if (lStartPos != startPosition)
            {
                //返回当前文件大小，通知浏览器从此位置开始上传
                endPosition = lStartPos - 1;
                return false;
            }

            byte[] nbytes = new byte[endPosition - startPosition + 1];
            int result = file.InputStream.Read(nbytes, 0, nbytes.Length);
            FastDFSClient.AppendFile(group, filename, nbytes);

            #region 作废
            /*
            byte[] nbytes = new byte[1024];
            int nReadSize = 0;
            nReadSize = file.InputStream.Read(nbytes, 0, nbytes.Length);
            while (nReadSize > 0)
            {
                FastDFSClient.AppendFile(group, filename, nbytes);
                nReadSize = file.InputStream.Read(nbytes, 0, nReadSize);
            }
            */
            #endregion

            return true;
        }

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="group"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public FDFSFileInfo GetFileInfo(string group, string filename)
        {
            //获取远程文件系统卷
            StorageNode node = FastDFSClient.GetStorageNode(group);
            FDFSFileInfo fileInfo = null;
            try
            {
                fileInfo = FastDFSClient.GetFileInfo(node, filename);
            }
            catch { throw new Exception("远程文件不存在，卷：" + group + "；文件名：" + filename); }
            return fileInfo;
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="group"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public void DelFile(string group, string filename)
        {
            FastDFSClient.RemoveFile(group, filename);
        }
    }
}
