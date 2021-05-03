#include <iostream>

#include "include/cef_app.h"
#include "include/cef_client.h"
#include "include/cef_render_handler.h"

#include <iostream> // andi: Debugging
#include <sys/types.h>
#include <sys/stat.h>

// socket
#include <stdio.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <string.h>
#define PORT 8888

// debug file
#include <fstream>
#include <chrono>

class OSRHandler : public CefRenderHandler {
private:
    int renderWidth;
    int renderHeight;
    int sock;
    int framecount = 0;
   
public:
    OSRHandler(int width, int height, int socket) {
        renderWidth = width;
        renderHeight = height;
        sock = socket;
    }
   
    void setRenderSize(int width, int height) {
        renderWidth = width;
        renderHeight = height;
    }
   
    void GetViewRect(CefRefPtr<CefBrowser> browser, CefRect &rect) {
        rect = CefRect(0, 0, renderWidth, renderHeight);
    }
   
    void OnPaint(CefRefPtr<CefBrowser> browser, PaintElementType type, const RectList &dirtyRects, const void *buffer, int width, int height) {
        unsigned char* img = (unsigned char*)buffer;
        printf("frame rendered (pixel[0]: (%d %d %d - %d)\n", img[2], img[1], img[0], img[3]);

        // send over socket
        int32 data_size = width*height*4;
        std::cout << "sending header: " << data_size << std::endl;
        send(sock, &data_size, 4, 0); // dirty?
        std::cout << "Writing buffer (" << data_size << ") to socket "  << " | " << "w x h: " << width << "x" << height << std::endl;
        send(sock, buffer, data_size, 0);

        // debug to file
        FILE* pFile;
        std::string filename;
        filename.append("buffer_");
        filename.append(std::to_string(framecount));
        filename.append(".data");
        pFile = fopen(filename.c_str(), "wb");
        framecount++;
        //pFile = fopen("cef_pipe", "wb+");
        fwrite(buffer, 1, width*height*4, pFile);
    }
   
    IMPLEMENT_REFCOUNTING(OSRHandler);
   
};

class BrowserClient : public CefClient {
private:
    CefRefPtr<CefRenderHandler> m_renderHandler;
   
public:
    BrowserClient(OSRHandler *renderHandler) {
        m_renderHandler = renderHandler;
    }
   
    virtual CefRefPtr<CefRenderHandler> GetRenderHandler() {
        return m_renderHandler;
    }
   
    IMPLEMENT_REFCOUNTING(BrowserClient);
};

void sendfile(int socket, const char* filename){
    char * memblock = new char [0];
    std::streampos size;

    std::ifstream file (filename, std::ios::in|std::ios::binary|std::ios::ate);
    if (file.is_open()){
        size = file.tellg();
        memblock = new char [size];
        file.seekg (0, std::ios::beg);
        file.read (memblock, size);
        file.close();

        
        int32 data_size = static_cast<int32>(size);
        std::cout << "sending header: " << data_size  << std::endl;;
        send(socket, &data_size, 4, 0); // dirty?
        std::cout << "sending file: " << filename << " size: " << size  << std::endl;;
        send(socket, memblock, size, 0);
        delete[] memblock;
    }else{
        std::cout << "file not open";
    }
}

int main(int argc, char* argv[]) {
    //CefMainArgs main_args(::GetModuleHandle(NULL));
    CefMainArgs main_args(argc, argv);
   
    int exit_code = CefExecuteProcess(main_args, NULL, NULL);
    if (exit_code >= 0)
      return exit_code;

    // SOCKET
    int sock = 0;//, valread;
    struct sockaddr_in serv_addr;
    //char buffer[1024] = {0};
    if ((sock = socket(AF_INET, SOCK_STREAM, 0)) < 0)
    {
        printf("\n Socket creation error \n");
        return -1;
    }
   
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_port = htons(PORT);
       
    // Convert IPv4 and IPv6 addresses from text to binary form
    if(inet_pton(AF_INET, "127.0.0.1", &serv_addr.sin_addr)<=0) 
    {
        printf("\nInvalid address/ Address not supported \n");
        return -1;
    }
   
    if (connect(sock, (struct sockaddr *)&serv_addr, sizeof(serv_addr)) < 0)
    {
        printf("\nConnection to socket failed \n");
        return -1;
    }
    // END SOCKET

    // DEBUG SEND FILE
    //sendfile(sock, "/tmp/600x400_2.data");
    //sendfile(sock, "/tmp/600x400.data");
    //sendfile(sock, "/tmp/600x400_2.data");
   
    CefSettings settings;
    CefInitialize(main_args, settings, NULL, NULL);
   
   
    CefBrowserSettings browserSettings;
    CefWindowInfo window_info;
    window_info.SetAsWindowless(0);
   
    OSRHandler* osrHandler = new OSRHandler(600, 400, sock); // TODO cmd argument
    CefRefPtr<BrowserClient> browserClient = new BrowserClient(osrHandler);
   

    //CefRefPtr<CefBrowser> browser = CefBrowserHost::CreateBrowserSync(window_info, browserClient.get(), "http://www.google.com", browserSettings, NULL);
    // Create the first browser window.
    CefBrowserHost::CreateBrowser(window_info, browserClient.get(), "https://duckduckgo.com/", browserSettings, nullptr, nullptr);
   
    CefRunMessageLoop();
    CefShutdown();

    close(sock);
    return 0;
}