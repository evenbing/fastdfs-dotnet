using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using FastDFS.Client;

namespace FastDFS_WebDemo
{
    public partial class upload : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            this.Image1.Visible = false;
            if (!this.FileUpload1.HasFile) return;

            string[] fileExtN = this.FileUpload1.FileName.Split('.');
            string ExtN = fileExtN.Length == 1 ? "noknow" : fileExtN[fileExtN.Length - 1];
            byte[] img = this.FileUpload1.FileBytes;
            string filename;
            string groupname = "group1";
            bool flag = FastDFSHelper.Current.Upload(groupname, img, ExtN, out filename);

            this.Image1.Visible = true;
            this.Image1.ImageUrl = "http://192.168.14.214/" + groupname + "/" + filename;
        }
    }
}