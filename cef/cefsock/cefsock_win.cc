#include <iostream>

#include "include/cef_app.h"
#include "include/cef_client.h"
#include "include/cef_render_handler.h"

#include <iostream>
#include <sys/types.h>
#include <sys/stat.h>

#include <stdio.h>
//#include <sys/socket.h>
//#include <arpa/inet.h>
//#include <unistd.h>
#include <string.h>

#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <stdlib.h>
// Need to link with Ws2_32.lib, Mswsock.lib, and Advapi32.lib
#pragma comment(lib, "Ws2_32.lib")
#pragma comment(lib, "Mswsock.lib")
#pragma comment(lib, "AdvApi32.lib")
#define DEFAULT_BUFLEN 512

#define PORT "8888"
#define TARGET_IP "127.0.0.1"
#define BROWSER_WIDTH 600
#define BROWSER_HEIGHT 400

// debug file
#include <fstream>

class OSRHandler : public CefRenderHandler {
private:
    int renderWidth;
    int renderHeight;
    int sock;
   
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
        //printf("frame rendered");

        // send over socket
        // first a header specifying the size of the following frame
        int32 data_size = width*height*4; // bgra
        int32* data_size_ptr = &data_size;
        std::cout << "sending header: " << data_size << std::endl;
        send(sock, reinterpret_cast<char*>(data_size_ptr), 4, 0);  // dirty?
        // now send the frame itself
        std::cout << "Writing buffer (" << data_size << ") to socket "  << " | " << "w x h: " << width << "x" << height << std::endl;
        send(sock, static_cast<const char*>(buffer), data_size, 0);
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

int main(int argc, char* argv[]) {
    CefMainArgs main_args(::GetModuleHandle(NULL));
    //CefMainArgs main_args(argc, argv);
   
    int exit_code = CefExecuteProcess(main_args, NULL, NULL);
    if (exit_code >= 0)
      return exit_code;

    // SOCKET
    /*
    int sock = 0;
    struct sockaddr_in serv_addr;
    if ((sock = socket(AF_INET, SOCK_STREAM, 0)) < 0)
    {
        printf("\n Socket creation error \n");
        return -1;
    }
   
    serv_addr.sin_family = AF_INET;
    serv_addr.sin_port = htons(PORT);
       
    // Convert IPv4 and IPv6 addresses from text to binary form
    if(inet_pton(AF_INET, TARGET_IP, &serv_addr.sin_addr)<=0) 
    {
        printf("\nInvalid address/ Address not supported \n");
        return -1;
    }
   
    if (connect(sock, (struct sockaddr *)&serv_addr, sizeof(serv_addr)) < 0)
    {
        printf("\nConnection to socket failed \n");
        return -1;
    }
    */

    WSADATA wsaData;
    SOCKET ConnectSocket = INVALID_SOCKET;
    struct addrinfo *result = NULL, *ptr = NULL, hints;
    int iResult;
    // Initialize Winsock
    iResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
    if (iResult != 0) {
      printf("WSAStartup failed with error: %d\n", iResult);
      return 1;
    }
    ZeroMemory(&hints, sizeof(hints));
    hints.ai_family = AF_UNSPEC;
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_protocol = IPPROTO_TCP;

    // Resolve the server address and port
    iResult = getaddrinfo(TARGET_IP, PORT, &hints, &result);
    if (iResult != 0) {
      printf("getaddrinfo failed with error: %d\n", iResult);
      WSACleanup();
      return 1;
    }

    // Attempt to connect to an address until one succeeds
    for (ptr = result; ptr != NULL; ptr = ptr->ai_next) {
      // Create a SOCKET for connecting to server
      ConnectSocket =
          socket(ptr->ai_family, ptr->ai_socktype, ptr->ai_protocol);
      if (ConnectSocket == INVALID_SOCKET) {
        printf("socket failed with error: %ld\n", WSAGetLastError());
        WSACleanup();
        return 1;
      }

      // Connect to server.
      iResult = connect(ConnectSocket, ptr->ai_addr, (int)ptr->ai_addrlen);
      if (iResult == SOCKET_ERROR) {
        closesocket(ConnectSocket);
        ConnectSocket = INVALID_SOCKET;
        continue;
      }
      break;
    }

    freeaddrinfo(result);

    if (ConnectSocket == INVALID_SOCKET) {
      printf("Unable to connect to server!\n");
      WSACleanup();
      return 1;
    }

    // END SOCKET

    // DEBUG SEND FILE
    // TODO move/remove
    //sendfile(sock, "/tmp/600x400_2.data");
    //sendfile(sock, "/tmp/600x400.data");
    //sendfile(sock, "/tmp/600x400_2.data");
   
    CefSettings settings;
    CefInitialize(main_args, settings, NULL, NULL);
   
    CefBrowserSettings browserSettings;
    CefWindowInfo window_info;
    window_info.SetAsWindowless(0);

    // use non-cef cmd args to configure
    CefRefPtr<CefCommandLine> command_line = CefCommandLine::GetGlobalCommandLine();
    CefCommandLine::ArgumentList args;
    command_line->GetArguments(args);
    std::string url = "https://www.duckduckgo.com";
    int width = BROWSER_WIDTH;
    int height = BROWSER_HEIGHT;
    if(command_line->HasArguments()){
        url = args[0].ToString();
        if(args.size() > 1){
            width = std::stoi(args[1].ToString());
        }
        if(args.size() > 2){
            height = std::stoi(args[2].ToString());
        }
    }else{
        std::cout << "No remaining args, using default url" << std::endl;
    }
    std::cout << "URL: " << url << " | WxH=" << width << "x" << height << std::endl;

    OSRHandler* osrHandler = new OSRHandler(width, height, sock);
    CefRefPtr<BrowserClient> browserClient = new BrowserClient(osrHandler);
   
    // Create the first browser window.
    CefBrowserHost::CreateBrowser(window_info, browserClient.get(), url, browserSettings, nullptr, nullptr);
   
    CefRunMessageLoop();
    CefShutdown();

    closesocket(ConnectSocket);
    return 0;
}
