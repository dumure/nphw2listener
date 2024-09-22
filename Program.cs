using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

var ip = IPAddress.Loopback;
var port = 27001;

Socket listener = new Socket(
    AddressFamily.InterNetwork,
    SocketType.Dgram,
    ProtocolType.Udp);
EndPoint connEp = new IPEndPoint(ip, port);
listener.Bind(connEp);

EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
const int oneFragmentSize = 10000;



while (true)
{
    var bufferBytes = new byte[100];
    var bufferLength = listener.ReceiveFrom(bufferBytes, ref endPoint);
    var message = Encoding.UTF8.GetString(bufferBytes, 0, bufferLength);
    if (message == "SCREENSHOT")
    {
        var screenshot = CaptureScreenshot();
        byte[] screenshotBytes = BitmapToByteArray(screenshot);
        int screenshotSize = screenshotBytes.Length;
        int fragmentsCount = (screenshotSize + oneFragmentSize - 1) / oneFragmentSize;
        byte[] fragmentsCountBytes = BitConverter.GetBytes(fragmentsCount);
        byte[] screenshotSizeBytes = BitConverter.GetBytes(screenshotBytes.Length);
        listener.SendTo(screenshotSizeBytes, endPoint);
        int offset = 0;
        for (int i = 0; i < fragmentsCount; i++)
        {
            if (i == fragmentsCount - 1)
            {

                byte[] fragmentBytes = new byte[screenshotSize - offset];
                Array.Copy(screenshotBytes, offset, fragmentBytes, 0, fragmentBytes.Length);
                listener.SendTo(fragmentBytes, endPoint);
                var answerBytes = new byte[1000];
                var answerLength = listener.ReceiveFrom(answerBytes, ref endPoint);
                var answer = Encoding.UTF8.GetString(answerBytes, 0, answerLength);
            }
            else
            {
                byte[] fragmentBytes = new byte[oneFragmentSize];
                Array.Copy(screenshotBytes, offset, fragmentBytes, 0, fragmentBytes.Length);
                listener.SendTo(fragmentBytes, endPoint);
                var answerBytes = new byte[1000];
                var answerLength = listener.ReceiveFrom(answerBytes, ref endPoint);
                var answer = Encoding.UTF8.GetString(answerBytes, 0, answerLength);
                offset += oneFragmentSize;
            }
        }
    }
}


Bitmap CaptureScreenshot()
{
    Rectangle bounds = Screen.PrimaryScreen.Bounds;
    Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

    using (Graphics g = Graphics.FromImage(bitmap))
    {
        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
    }

    return bitmap;
}

byte[] BitmapToByteArray(Bitmap bitmap)
{
    using (MemoryStream ms = new MemoryStream())
    {
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}