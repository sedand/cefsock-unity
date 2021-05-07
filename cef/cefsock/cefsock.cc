#include <iostream>

#include "include/cef_app.h"
#include "include/cef_client.h"
#include "include/cef_render_handler.h"

#include <sys/stat.h>
#include <sys/types.h>
#include <iostream>

#include <arpa/inet.h>
#include <stdio.h>
#include <string.h>
#include <sys/socket.h>
#include <unistd.h>

#include <thread>

#define PORT 8888
#define TARGET_IP "127.0.0.1"
#define BROWSER_WIDTH 600
#define BROWSER_HEIGHT 400

#define MOUSE_DOWN 0
#define MOUSE_UP 1

// debug file
#include <fstream>

CefRefPtr<CefBrowser> m_browser = NULL;

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

  void GetViewRect(CefRefPtr<CefBrowser> browser, CefRect& rect) {
    rect = CefRect(0, 0, renderWidth, renderHeight);
  }

  void OnPaint(CefRefPtr<CefBrowser> browser,
               PaintElementType type,
               const RectList& dirtyRects,
               const void* buffer,
               int width,
               int height) {
    m_browser = browser;
    // printf("frame rendered");

    // send over socket
    // first a header specifying the size of the following frame
    int32 data_size = width * height * 4;  // bgra
    // std::cout << "sending header: " << data_size << std::endl;
    send(sock, &data_size, 4, 0);  // dirty?
    // now send the frame itself
    // std::cout << "Writing buffer (" << data_size << ") to socket "  << " | "
    // << "w x h: " << width << "x" << height << std::endl;
    send(sock, buffer, data_size, 0);
  }

  IMPLEMENT_REFCOUNTING(OSRHandler);
};

class BrowserClient : public CefClient, public CefLifeSpanHandler {
 private:
  CefRefPtr<CefRenderHandler> m_renderHandler;

 public:
  BrowserClient(OSRHandler* renderHandler) { m_renderHandler = renderHandler; }

  virtual CefRefPtr<CefRenderHandler> GetRenderHandler() {
    return m_renderHandler;
  }

  IMPLEMENT_REFCOUNTING(BrowserClient);
};

// DEBUG function for testing, sends a given file
// TODO move/remove
void sendfile(int socket, const char* filename) {
  char* memblock = new char[0];
  std::streampos size;

  std::ifstream file(filename, std::ios::in | std::ios::binary | std::ios::ate);
  if (file.is_open()) {
    size = file.tellg();
    memblock = new char[size];
    file.seekg(0, std::ios::beg);
    file.read(memblock, size);
    file.close();

    int32 data_size = static_cast<int32>(size);
    std::cout << "sending header: " << data_size << std::endl;
    ;
    send(socket, &data_size, 4, 0);  // dirty?
    std::cout << "sending file: " << filename << " size: " << size << std::endl;
    ;
    send(socket, memblock, size, 0);
    delete[] memblock;
  } else {
    std::cout << "file not open";
  }
}

void readSocket(int socket) {
  std::cout << "Socket read thread is running" << std::endl;
  int type;
  int x;
  int y;
  while (true) {
    int nread;

    // TYPE
    nread = read(socket, &type, 4);
    std::cout << "Read " << nread << ": int type=" << type << std::endl;

    // X
    nread = read(socket, &x, 4);
    std::cout << "Read " << nread << ": int x=" << x << std::endl;

    // Y
    nread = read(socket, &y, 4);
    // char key = char(headerbuffer[0]);
    std::cout << "Read " << nread << ": int y=" << y << std::endl;

    if (m_browser != NULL) {
      /*
      CefKeyEvent event;
      event.windows_key_code = key;
      event.native_key_code = key;
      event.type = KEYEVENT_CHAR;
      event.modifiers = 0;
      //keyEvent.unmodified_character = chr;
      event.is_system_key = false;
      event.focus_on_editable_field = true;

      CefKeyEvent event2;
      event2.type = KEYEVENT_RAWKEYDOWN;
      m_browser->GetHost()->SendKeyEvent(event2);
      m_browser->GetHost()->SendKeyEvent(event);
      */

      CefMouseEvent mouseEvent;
      mouseEvent.x = x;
      mouseEvent.y = y;
      std::cout << "CefMouseEvent type=" << type << " x=" << mouseEvent.x
                << " y=" << mouseEvent.y << std::endl;

      if (type == MOUSE_DOWN) {
        m_browser->GetHost()->SendMouseClickEvent(mouseEvent, MBT_LEFT, false,
                                                  1);
      } else if (type == MOUSE_UP) {
        m_browser->GetHost()->SendMouseClickEvent(mouseEvent, MBT_LEFT, true,
                                                  1);
      }
    } else {
      std::cerr << "Error, m_browser is NULL" << std::endl;
    }
  }

  // TESTING KEY
  /*
  CefKeyEvent event;
  event.windows_key_code = 64;
  event.native_key_code = 64;
  //keyEvent.type = 'a';
  event.modifiers = 0;
  //keyEvent.unmodified_character = chr;
  event.is_system_key = false;
  event.focus_on_editable_field = true;
  CefBrowserHost::GetBrowser().GetHost().SendKeyEvent(event);
  */
}

int main(int argc, char* argv[]) {
  CefMainArgs main_args(argc, argv);

  int exit_code = CefExecuteProcess(main_args, NULL, NULL);
  if (exit_code >= 0)
    return exit_code;

  // SOCKET
  int sock = 0;
  struct sockaddr_in serv_addr;
  if ((sock = socket(AF_INET, SOCK_STREAM, 0)) < 0) {
    printf("\n Socket creation error \n");
    return -1;
  }

  serv_addr.sin_family = AF_INET;
  serv_addr.sin_port = htons(PORT);

  // Convert IPv4 and IPv6 addresses from text to binary form
  if (inet_pton(AF_INET, TARGET_IP, &serv_addr.sin_addr) <= 0) {
    printf("\nInvalid address/ Address not supported \n");
    return -1;
  }

  if (connect(sock, (struct sockaddr*)&serv_addr, sizeof(serv_addr)) < 0) {
    printf("\nConnection to socket failed \n");
    return -1;
  } else {
    printf("Connected to socket\n");
  }

  // start reading thread
  std::thread th1(readSocket, sock);

  // END SOCKET

  // DEBUG SEND FILE
  // TODO move/remove
  // sendfile(sock, "/tmp/600x400_2.data");
  // sendfile(sock, "/tmp/600x400.data");
  // sendfile(sock, "/tmp/600x400_2.data");

  CefSettings settings;
  CefInitialize(main_args, settings, NULL, NULL);

  CefBrowserSettings browserSettings;
  CefWindowInfo window_info;
  window_info.SetAsWindowless(0);

  // use non-cef cmd args to configure
  CefRefPtr<CefCommandLine> command_line =
      CefCommandLine::GetGlobalCommandLine();
  CefCommandLine::ArgumentList args;
  command_line->GetArguments(args);
  std::string url = "https://www.duckduckgo.com";
  int width = BROWSER_WIDTH;
  int height = BROWSER_HEIGHT;
  if (command_line->HasArguments()) {
    url = args[0].ToString();
    if (args.size() > 1) {
      width = std::stoi(args[1].ToString());
    }
    if (args.size() > 2) {
      height = std::stoi(args[2].ToString());
    }
  } else {
    std::cout << "No remaining args, using default url" << std::endl;
  }
  std::cout << "URL: " << url << " | WxH=" << width << "x" << height
            << std::endl;

  OSRHandler* osrHandler = new OSRHandler(width, height, sock);
  CefRefPtr<BrowserClient> browserClient = new BrowserClient(osrHandler);

  // Create the first browser window.
  CefBrowserHost::CreateBrowser(window_info, browserClient.get(), url,
                                browserSettings, nullptr, nullptr);

  CefRunMessageLoop();
  CefShutdown();

  // TODO STOP reading thread
  close(sock);
  return 0;
}