using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


class Seel{
    private HttpListener listener;
    
    private string url;
    private string pageData = @"
        <p>Hello World</p>
        <script>window.onunload = () => {fetch('/shutdown', {method: 'POST'})}</script>
    ";

    public Seel(string path){
        int port = GetAvailablePort();
        url = $"http://localhost:{port}/";
        
        try{
            pageData = File.ReadAllText(path);
        }catch{

        }
    }

    private static int GetAvailablePort(){
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
    
    private async Task HandleIncomingConnections(){
        bool runServer = true;

        while (runServer){
            HttpListenerContext ctx = await listener.GetContextAsync();

            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse res = ctx.Response;

            if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown")){
                runServer = false;
            }

            byte[] data = Encoding.UTF8.GetBytes(pageData);
            res.ContentType = "text/html";
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = data.LongLength;

            await res.OutputStream.WriteAsync(data, 0, data.Length);
            res.Close();
        }
    }

    private void RunBackendLoop(){
        listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();

        Task listenTask = HandleIncomingConnections();
        listenTask.GetAwaiter().GetResult();

        listener.Close();
    }

    private void OpenGUI(){
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;


        List<string> browserPaths = new List<string>();
        browserPaths.Add(GetChromePath());
        browserPaths.Add(GetEdgePath());

        foreach(string browserPath in browserPaths){
            if(browserPath != null){
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/C \"\"{browserPath}\" --app=\"{url}\"\"";
            }
        }
        
        process.StartInfo = startInfo;
        process.Start();
        process.Close();
    }

    private static string GetChromePath(){
        string path = Microsoft.Win32.Registry.GetValue(@"HKEY_CLASSES_ROOT\ChromeHTML\shell\open\command", null, null) as string;
        if (path != null){
            string[] split = path.Split('\"');
            path = split.Length >= 2 ? split[1] : null;
        }
        return path;
    }

    private static string GetEdgePath(){
        string path = "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe";
        if(File.Exists(path)){
            return path;
        }else{
            return null;
        }
    }

    public static void Main(string[] args){
        // hide window of current process
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 0;
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_HIDE);

        string pathToExecutable = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string path = $"{pathToExecutable}/index.html";

        Seel seel = new Seel(path);
        
        seel.OpenGUI();
        seel.RunBackendLoop();
    }
}
