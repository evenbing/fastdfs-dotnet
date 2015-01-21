<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="upload.aspx.cs" Inherits="FastDFS_WebDemo.upload" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <br />
        FastDFS上传图片演示Demo<br />
        <br />
        需要上传的图片：<asp:FileUpload ID="FileUpload1" runat="server" />
        <br />
        <br />
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="上传" />
        <br />
        <br />
        上传的图片：<br />
        <asp:Image ID="Image1" runat="server" />
    
    </div>
    </form>
</body>
</html>
