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
using UnityEngine.EventSystems;

public class CefsockUnity : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private int MOUSE_DOWN = 0;
    private int MOUSE_UP = 1;

    public int imageWidth = 600;
    public int imageHeight = 400;
    public string url = "https://duckduckgo.com";

    public string cefFolderName = "cefsock";
    private string cefExecutableName = "cefsock";
    private diag.Process cefProcess;

    private Thread socketThread;
    private Socket listener;
    private Socket client;
    private bool listening;
    public int port = 8888;

    public bool verbose = false;

    protected byte[] frameBuffer;
    protected bool frameBufferChanged;
    private int bufferSize;
    protected Texture2D browserTexture;
    protected RawImage targetImage;
    protected Rect targetImageRect;

    private Webserver ws;
    public int webServerPort = 8889;

    // Start is called before the first frame update
    void Start()
    {
        // init images
        targetImage = GetComponent<RawImage>();
        targetImageRect = targetImage.GetComponent<RectTransform>().rect;

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

        while (!listening)
        {
            Debug.Log("Waiting for socket ...");
            Thread.Sleep(50);
        }

        Debug.Log("Socket is listening.");
        Debug.Log("Starting Cef ...");
        startCef();
        Thread.Sleep(250);
        Debug.Log("Cef is running.");

        // start webserver
        //ws = new Webserver();
        //ws.Start(webServerPort);
        //Debug.Log("Webserver listening on 127.0.0.1:" + webServerPort);
    }

    void startCef()
    {
        string cefPathExec, wd;
        if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
            wd = cefFolderName;
            Debug.Log("Environment.CurrentDirectory: " + Environment.CurrentDirectory);
            cefPathExec = Path.Combine(Environment.CurrentDirectory, "cefsock", cefExecutableName + ".exe");
        }
        else
        {
            wd = "./" + cefFolderName;
            cefPathExec = Path.Combine(wd, cefExecutableName);
        }

        string procArgs = url + " " + imageWidth + " " + imageHeight;
        Debug.Log("Cef arguments: " + procArgs);

        // Start the process, hide it, and listen to its output
        var processInfo = new diag.ProcessStartInfo();
        processInfo.Arguments = procArgs;
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
                                 { Debug.Log("[CEF OUT] " + e.Data); });
        cefProcess.ErrorDataReceived += new diag.DataReceivedEventHandler((sender, e) =>
                                 { Debug.Log("[CEF ERR] " + e.Data); });
        cefProcess.BeginOutputReadLine();
        cefProcess.BeginErrorReadLine();
        //string output = cefProcess.StandardOutput.ReadToEnd();  
        //Debug.Log(output);
        //process.OutputDataReceived += Process_OutputDataReceived;
    }

    void debugLog(string message)
    {
        if (verbose)
        {
            Debug.Log(message);
        }
    }

    void AsyncReceive(Socket client)
    {
        Task.Run(new Action(() =>
            {
                /*
                while(pipeStream != null && pipeStream.IsConnected){
                    pipeStream.Read(frameBuffer, 0, bufferSize);
                    //Debug.Log("Received frameBuffer from server");
                    frameBufferChanged = true;
                }
                */
                while (client != null && client.Connected)
                {
                    debugLog("Receiving next frame header...");
                    byte[] header = new byte[4];
                    int receivedHeaderBytes = client.Receive(header, 0, 4, SocketFlags.None);
                    debugLog("Got " + receivedHeaderBytes + " bytes");
                    int frameLength = BitConverter.ToInt32(header, 0);
                    debugLog("Got next frame length: " + frameLength);

                    byte[] loopBuffer = new byte[frameLength];
                    //int bytesReceivedTotal = 0;
                    int bytesReceived = 0;
                    while ((bytesReceived += client.Receive(loopBuffer, bytesReceived, frameLength - bytesReceived, SocketFlags.None)) > 0)
                    {
                        //bytesReceivedTotal += bytesReceived;
                        debugLog("Received bytes: " + bytesReceived);// + " | Total: "+bytesReceivedTotal);

                        if (bytesReceived >= frameLength)
                        { // TODO == ?
                            debugLog("Frame receive complete");

                            Buffer.BlockCopy(loopBuffer, 0, frameBuffer, 0, frameLength);
                            debugLog("Copied frame to framebuffer");
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
        if (frameBufferChanged)
        {
            browserTexture.LoadRawTextureData(frameBuffer);
            browserTexture.Apply();
            frameBufferChanged = false;

            //File.WriteAllBytes("framebuffer-" + DateTime.Now.Ticks + ".data", frameBuffer);
            //Debug.Log("Wrote framebuffer to file");
        }

        // TEST
        /*
        if (Input.anyKey)
        {
            if (client != null && client.Connected)
            {
                Debug.Log("Sending frame header...");
                byte[] header = BitConverter.GetBytes(65);
                Debug.Log("header len: " + header.Length);
                client.Send(header);
            }
        }
        */
    }

    private byte[] buildMouseMessage(int type, float x, float y)
    {
        byte[] message = new byte[4 * 3];
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, message, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes((int)x), 0, message, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes((int)y), 0, message, 8, 4);
        return message;
    }

    private Vector2 changeCoordinates(Vector3 localHit)
    {
        // change coordinate to cef top left = 0,0
        float cefY = -(localHit.y - targetImageRect.height);
        float cefX = localHit.x;

        // normalize to cef image size (raw image size may differ)
        float ratioY = targetImageRect.height / imageHeight;
        float ratioX = targetImageRect.width / imageWidth;
        float cefYnorm = (cefY / ratioY);
        float cefXnorm = (cefX / ratioX);

        return new Vector2(cefXnorm, cefYnorm);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log(eventData);
        Vector3 localHit = transform.InverseTransformPoint(eventData.pressPosition);

        Vector2 cefHit = changeCoordinates(localHit);

        //Debug.Log(localHit + " | cefY: " + cefY + "/" + cefYnorm + "| cefX: " + cefX + "/" + cefXnorm);
        if (client != null && client.Connected)
        {
            byte[] message = buildMouseMessage(MOUSE_DOWN, cefHit.x, cefHit.y);
            client.Send(message);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log(eventData);
        Vector3 localHit = transform.InverseTransformPoint(eventData.pressPosition);

        Vector2 cefHit = changeCoordinates(localHit);

        //Debug.Log(localHit + " | cefY: " + cefY + "/" + cefYnorm + "| cefX: " + cefX + "/" + cefXnorm);
        if (client != null && client.Connected)
        {
            byte[] message = buildMouseMessage(MOUSE_UP, cefHit.x, cefHit.y);
            client.Send(message);
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

                client = listener.Accept(); // TODO manage multiple connections?
                Debug.Log("Client Connected");

                AsyncReceive(client);
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

        if (client != null && client.Connected)
        {
            client.Disconnect(false);
            Debug.Log("Disconnected!");
        }
    }

    void OnDisable()
    {
        ws.Stop();

        if (cefProcess != null && !cefProcess.HasExited)
        {
            cefProcess.Kill();
        }
        stopServer();
    }
}
