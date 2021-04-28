using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using diag = System.Diagnostics;

public class CEF_Client : MonoBehaviour
{
    public string cefPath = "./Cef";
    public string pipeName = "cef_pipe";
    public int bufferSize = 800 * 531 * 4; // TODO

    private NamedPipeClientStream pipeStream;

    protected byte[] frameBuffer;
    protected bool frameBufferChanged;
    protected Texture2D browserTexture;
    protected RawImage targetImage;

    // Start is called before the first frame update
    void Start()
    {
        // init images
        targetImage = GetComponent<RawImage>();
        browserTexture = new Texture2D(800, 531, TextureFormat.BGRA32, false);

        targetImage.texture = browserTexture;
        targetImage.uvRect = new Rect(0f, 0f, 1f, -1f);

        frameBuffer = new byte[bufferSize];
        frameBufferChanged = false;

        Debug.Log("Starting Cef...");
        startCef();
        Thread.Sleep(250);
        Debug.Log("Cef is running, connecting pipe & starting async receive...");

        // connect pipe - blocking
        string baseDir = new DirectoryInfo(Application.dataPath).Parent.FullName;
        string cefPath = baseDir + "/Cef/cef_pipe";
        Debug.Log("Looking for pipe at: "+cefPath);
        pipeStream = new NamedPipeClientStream(".", cefPath, PipeDirection.InOut);
        Debug.Log("Attempting to connect to pipe...");
        pipeStream.Connect(500);
        Debug.Log("Connected to pipe.");

        AsyncReceive();
    }

    void startCef(){
        //string cefPath = @"./Cef";
        string cefPathExec = cefPath + "/cefclient";

        // Start the process, hide it, and listen to its output
        var processInfo = new diag.ProcessStartInfo();
        processInfo.Arguments = "--off-screen-rendering-enabled";
        processInfo.CreateNoWindow = true;
        processInfo.FileName = cefPathExec;
        processInfo.WorkingDirectory = cefPath;
        processInfo.UseShellExecute = false;
        processInfo.RedirectStandardInput = true;
        processInfo.RedirectStandardOutput = true;
        processInfo.WindowStyle = diag.ProcessWindowStyle.Hidden;

        var process = diag.Process.Start(processInfo);
        //process.ErrorDataReceived += Process_ErrorDataReceived;
        //process.OutputDataReceived += Process_OutputDataReceived;
    }

    void AsyncReceive(){
        Task.Run(new Action(() =>
            {
                while(pipeStream != null && pipeStream.IsConnected){
                    pipeStream.Read(frameBuffer, 0, bufferSize);
                    //Debug.Log("Received frameBuffer from server");
                    frameBufferChanged = true;
                }
            }));
    }

    // Update is called once per frame
    void Update()
    {
        if(frameBufferChanged){
            browserTexture.LoadRawTextureData(frameBuffer);
            browserTexture.Apply();
            frameBufferChanged = false;
        }

    }
}
