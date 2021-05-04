using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using diag = System.Diagnostics;

public class CefsockUnity : MonoBehaviour
{
    public int imageWidth = 800;
    public int imageHeight = 600;

    public string cefFolderName = "Cefpipe";
    public string cefExecutableName = "cefpipe";
    private diag.Process cefProcess;

    private Thread socketThread;
    private Socket listener;
    private Socket handler;
    private bool listening;
    public int port = 8888;

    protected byte[] frameBuffer;
    protected bool frameBufferChanged;
    private int bufferSize;
    protected Texture2D browserTexture;
    protected RawImage targetImage;

    // Start is called before the first frame update
    void Start()
    {
        // init images
        targetImage = GetComponent<RawImage>();
        browserTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.BGRA32, false);

        targetImage.texture = browserTexture;
        targetImage.uvRect = new Rect(0f, 0f, 1f, -1f);

        bufferSize = imageWidth * imageHeight * 4;
        frameBuffer = new byte[bufferSize];
        frameBufferChanged = false;

        // start socket
        Debug.Log("Opening socket ...");
        listening = false;
        socketThread = new System.Threading.Thread(openSocket);
        socketThread.IsBackground = true;
        socketThread.Start();

        while(!listening){
            Debug.Log("Waiting for socket ...");
            Thread.Sleep(50);
        }

        Debug.Log("Socket is listening.");
        Debug.Log("Starting Cef ...");
        startCef();
        Thread.Sleep(250);
        Debug.Log("Cef is running.");

        //Debug.Log("Client is connected, starting async receive");
        //string baseDir = new DirectoryInfo(Application.dataPath).Parent.FullName;
        //AsyncReceive();

    }

    void startCef(){
        string wd = "./" + cefFolderName;
        string cefPathExec = wd + "/" + cefExecutableName; // TODO windows?

        // Start the process, hide it, and listen to its output
        var processInfo = new diag.ProcessStartInfo();
        //processInfo.Arguments = "--off-screen-rendering-enabled";
        processInfo.CreateNoWindow = true;
        processInfo.FileName = cefPathExec;
        processInfo.WorkingDirectory = wd;
        processInfo.UseShellExecute = false;
        processInfo.RedirectStandardInput = true;
        processInfo.RedirectStandardOutput = true;
        processInfo.RedirectStandardError = true;
        //processInfo.WindowStyle = diag.ProcessWindowStyle.Hidden;

        cefProcess = diag.Process.Start(processInfo);
        cefProcess.OutputDataReceived += new diag.DataReceivedEventHandler((sender, e) => 
                                 { Debug.Log("[CEF OUT] "+e.Data); });
        cefProcess.ErrorDataReceived += new diag.DataReceivedEventHandler((sender, e) => 
                                 { Debug.Log("[CEF ERR] "+e.Data); });
        cefProcess.BeginOutputReadLine();
        cefProcess.BeginErrorReadLine();
        //string output = cefProcess.StandardOutput.ReadToEnd();  
        //Debug.Log(output);
        //process.OutputDataReceived += Process_OutputDataReceived;
    }

    void AsyncReceive(Socket client){
        Task.Run(new Action(() =>
            {
                /*
                while(pipeStream != null && pipeStream.IsConnected){
                    pipeStream.Read(frameBuffer, 0, bufferSize);
                    //Debug.Log("Received frameBuffer from server");
                    frameBufferChanged = true;
                }
                */
                while (client != null && client.Connected){
                    Debug.Log("Receiving next frame header...");
                    byte[] header = new byte[4];
                    int receivedHeaderBytes = client.Receive(header, 0, 4, SocketFlags.None);
                    Debug.Log("Got "+receivedHeaderBytes+ " bytes");
                    int frameLength = BitConverter.ToInt32(header, 0);
                    Debug.Log("Got next frame length: "+frameLength);

                    byte[] loopBuffer = new byte[frameLength];
                    //int bytesReceivedTotal = 0;
                    int bytesReceived = 0;
                    while ((bytesReceived += client.Receive(loopBuffer, bytesReceived, frameLength-bytesReceived, SocketFlags.None)) > 0){
                        //bytesReceivedTotal += bytesReceived;
                        Debug.Log("Received bytes: "+bytesReceived);// + " | Total: "+bytesReceivedTotal);

                        if(bytesReceived >= frameLength){ // TODO == ?
                            Debug.Log("Frame receive complete");

                            Buffer.BlockCopy(loopBuffer, 0, frameBuffer, 0, frameLength);
                            Debug.Log("Copied frame to framebuffer");
                            frameBufferChanged = true;
                            break;
                        }
                    }
                    
                }

                /*
                byte[] buffer = new byte[bufferSize];
                int bytesBuffered = 0;
                while (client != null && client.Connected){
                    byte[] loopBuffer = new byte[64000];
                    int bytesNow = client.Receive(loopBuffer);
                    int bytesReceived = bytesBuffered + bytesNow;
                    Debug.Log("Now bytes: "+bytesNow+" | Total (this buffer): "+bytesReceived);

                    if(bytesReceived < bufferSize){
                        Debug.Log("Got incomplete buffer: "+bytesReceived);
                        Buffer.BlockCopy(loopBuffer, 0, buffer, bytesBuffered, bytesNow);
                        bytesBuffered += bytesNow;
                    }
                    else{
                        Debug.Log("Got a complete buffer: "+bytesReceived);

                        int leftbytes = bufferSize - bytesBuffered; // bytes missing to complete the current buffer
                        Buffer.BlockCopy(loopBuffer, 0, buffer, bytesBuffered, leftbytes); // copy this much to buffer
                        File.WriteAllBytes("buffer-" + DateTime.Now.Ticks + ".data", buffer);
                        //bytesBuffered += leftbytes;

                        Buffer.BlockCopy(buffer, 0, frameBuffer, 0, bufferSize); // copy completed buffer to framebuffer
                        frameBufferChanged = true;
                        Debug.Log("Reset receive buffer for next frame");
                        buffer = new byte[bufferSize]; // reset buffer to start next frame

                        int nextbytes = bytesNow - leftbytes;
                        if(nextbytes > 0){ // we already received bytes of the next frame
                            Debug.Log("Already got bytes of the next frame: "+nextbytes);
                            Buffer.BlockCopy(loopBuffer, leftbytes, buffer, 0, nextbytes); // copy to start of the buffer
                            bytesBuffered = nextbytes;
                        }
                    }

                    if (bytesNow <= 0){
                        Debug.Log("Received bytes <=0, closing connection");
                        client.Disconnect(true);
                    }

                    System.Threading.Thread.Sleep(1);
                }
                */
            }));
    }

    // Update is called once per frame
    void Update()
    {
        if(frameBufferChanged){
            browserTexture.LoadRawTextureData(frameBuffer);
            browserTexture.Apply();
            frameBufferChanged = false;

            File.WriteAllBytes("framebuffer-" + DateTime.Now.Ticks + ".data", frameBuffer);
            Debug.Log("Wrote framebuffer to file");
        }

    }

    private string getIPAddress()
    {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
            }

        }
        return localIP;
    }

    void openSocket()
    {
        // Data buffer for incoming data.
        byte[] bytes = new Byte[bufferSize];

        // host running the application.
        IPAddress[] ipArray = Dns.GetHostAddresses("127.0.0.1");
        IPEndPoint localEndPoint = new IPEndPoint(ipArray[0], port);

        // Create a TCP/IP socket.
        listener = new Socket(ipArray[0].AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and 
        // listen for incoming connections.

        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(10);
            listening = true;

            while (true)
            {

                // Blocking
                Debug.Log("Waiting for Connection");

                handler = listener.Accept(); // TODO manage multiple connections?
                Debug.Log("Client Connected");

                AsyncReceive(handler);
            }

        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    void stopServer()
    {
        //stop thread
        if (socketThread != null)
        {
            socketThread.Abort();
        }

        if (handler != null && handler.Connected)
        {
            handler.Disconnect(false);
            Debug.Log("Disconnected!");
        }
    }

    void OnDisable()
    {
        if(cefProcess != null && !cefProcess.HasExited){
            cefProcess.Kill();
        }
        stopServer();
    }
}
