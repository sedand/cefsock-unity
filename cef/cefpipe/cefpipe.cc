#include <iostream>

#include "include/cef_app.h"
#include "include/cef_client.h"
#include "include/cef_render_handler.h"

#include <iostream> // andi: Debugging
#include <sys/types.h>
#include <sys/stat.h>

class OSRHandler : public CefRenderHandler {
private:
    int renderWidth;
    int renderHeight;
   
public:
    OSRHandler(int width, int height) {
        renderWidth = width;
        renderHeight = height;
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

        // andi debugging buffer
        //unsigned char* pCharBuffer = static_cast<unsigned char*>(buffer);
        std::cout << "Writing buffer to pipe "  << " | " << "w x h: " << width << "x" << height << std::endl;

        FILE* pFile;
        //pFile = fopen("buffer.bgra", "wb");
        pFile = fopen("cef_pipe", "wb+");
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


int main(int argc, char* argv[]) {
    //CefMainArgs main_args(::GetModuleHandle(NULL));
    CefMainArgs main_args(argc, argv);
   
    int exit_code = CefExecuteProcess(main_args, NULL, NULL);
    if (exit_code >= 0)
      return exit_code;

    char const* fifo_path = "cef_pipe";
    mkfifo(fifo_path, 0666);
    std::cout << "Created fifo at " << fifo_path << std::endl;
   
    CefSettings settings;
    CefInitialize(main_args, settings, NULL, NULL);
   
   
    CefBrowserSettings browserSettings;
    CefWindowInfo window_info;
    window_info.SetAsWindowless(0);
   
    OSRHandler* osrHandler = new OSRHandler(800, 600);
    CefRefPtr<BrowserClient> browserClient = new BrowserClient(osrHandler);
   

    //CefRefPtr<CefBrowser> browser = CefBrowserHost::CreateBrowserSync(window_info, browserClient.get(), "http://www.google.com", browserSettings, NULL);
    // Create the first browser window.
    CefBrowserHost::CreateBrowser(window_info, browserClient.get(), "http://duckduckgo.com", browserSettings, nullptr, nullptr);
   
    CefRunMessageLoop();
   
   CefShutdown();
    return 0;
}