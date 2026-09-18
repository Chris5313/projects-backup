// preview.cpp — offscreen render of the GW2 overlay menu to a BMP.
// Drives the payload's real Overlay_* API (no game, no hooks) so the menu
// can be screenshotted exactly as it appears in-game.
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <dxgi.h>
#include <cstdio>
#include <cstdlib>
#include <cstdint>
#include "framework/functions.h"

// payload/overlay.cpp
bool Overlay_Init(void* swapchain, HWND hwnd);
bool Overlay_RenderFrame(void* swapchain);
void Overlay_ToggleMenu();

static bool write_bmp24(const char* path, const uint8_t* bgra, int w, int h)
{
    const int row = ((w * 3 + 3) / 4) * 4;
    const uint32_t dataSize = (uint32_t)row * h;
    const uint32_t fileSize = 54 + dataSize;
    uint8_t hdr[54];
    memset(hdr, 0, sizeof(hdr));
    hdr[0] = 'B'; hdr[1] = 'M';
    memcpy(hdr + 2, &fileSize, 4);
    const uint32_t off = 54; memcpy(hdr + 10, &off, 4);
    const uint32_t ih = 40;  memcpy(hdr + 14, &ih, 4);
    memcpy(hdr + 18, &w, 4);
    memcpy(hdr + 22, &h, 4);
    const uint16_t planes = 1, bpp = 24;
    memcpy(hdr + 26, &planes, 2);
    memcpy(hdr + 28, &bpp, 2);
    memcpy(hdr + 34, &dataSize, 4);

    FILE* f = fopen(path, "wb");
    if (!f) return false;
    fwrite(hdr, 1, 54, f);
    uint8_t* line = (uint8_t*)malloc(row);
    if (!line) { fclose(f); return false; }
    memset(line, 0, row);
    for (int y = h - 1; y >= 0; y--) {          // BMP is bottom-up
        const uint8_t* src = bgra + (size_t)y * w * 4;
        for (int x = 0; x < w; x++) {
            line[x * 3 + 0] = src[x * 4 + 0];   // B
            line[x * 3 + 1] = src[x * 4 + 1];   // G
            line[x * 3 + 2] = src[x * 4 + 2];   // R
        }
        fwrite(line, 1, row, f);
    }
    free(line);
    fclose(f);
    return true;
}

int main(int argc, char** argv)
{
    const char* out = argc > 1 ? argv[1] : "menu_preview.bmp";
    const int frames = argc > 2 ? atoi(argv[2]) : 240;
    setvbuf(stdout, nullptr, _IONBF, 0);

    WNDCLASSA wc = {};
    wc.lpfnWndProc = DefWindowProcA;
    wc.hInstance = GetModuleHandleA(nullptr);
    wc.lpszClassName = "GW2MenuPreview";
    RegisterClassA(&wc);
    HWND hwnd = CreateWindowExA(0, wc.lpszClassName, "GW2 Menu Preview",
        WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, 1600, 900,
        nullptr, nullptr, wc.hInstance, nullptr);

    DXGI_SWAP_CHAIN_DESC scd = {};
    scd.BufferCount = 2;
    scd.BufferDesc.Width = 1600;
    scd.BufferDesc.Height = 900;
    scd.BufferDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
    scd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    scd.OutputWindow = hwnd;
    scd.SampleDesc.Count = 1;
    scd.Windowed = TRUE;
    scd.SwapEffect = DXGI_SWAP_EFFECT_SEQUENTIAL; // BitBlt model: buffer 0 content survives Present for readback

    IDXGISwapChain* sc = nullptr;
    ID3D11Device* dev = nullptr;
    ID3D11DeviceContext* ctx = nullptr;
    const UINT flags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;
    HRESULT hr = D3D11CreateDeviceAndSwapChain(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr,
        flags, nullptr, 0, D3D11_SDK_VERSION, &scd, &sc, &dev, nullptr, &ctx);
    if (FAILED(hr))
        hr = D3D11CreateDeviceAndSwapChain(nullptr, D3D_DRIVER_TYPE_WARP, nullptr,
            flags, nullptr, 0, D3D11_SDK_VERSION, &scd, &sc, &dev, nullptr, &ctx);
    if (FAILED(hr) || !sc) { printf("device failed 0x%08lX\n", (unsigned long)hr); return 1; }
    printf("cp: device ok\n");
    if (!Overlay_Init(sc, hwnd)) { printf("Overlay_Init failed\n"); return 1; }
    printf("cp: overlay init ok\n");
    Overlay_ToggleMenu();
    // Show the Combat tab fully populated (what the user sees in-game)
    var->c_aimbot.aimbot = true;
    var->c_chams.enable = true;
    printf("cp: vars set ok\n");

    // argv[5] = "x,y" — simulate a left click at that screen position at frame 150
    // (e.g. on the accent-color swatch, to screenshot the opened picker popup)
    int clickX = -1, clickY = -1;
    if (argc > 5 && sscanf(argv[5], "%d,%d", &clickX, &clickY) == 2)
        printf("cp: will click at %d,%d on frame 150\n", clickX, clickY);

    if (argc > 4) {  // argv[4] = tab index 0..4 — jump straight to that tab
        var->c_selection.selection = atoi(argv[4]);
        var->c_selection.selection_active = atoi(argv[4]);
        var->c_selection.selection_alpha = 1.f;
        printf("cp: tab -> %d\n", atoi(argv[4]));
    }

    int click2X = -1, click2Y = -1;
    if (argc > 7 && sscanf(argv[7], "%d,%d", &click2X, &click2Y) == 2)
        printf("cp: will click at %d,%d on frame 250\n", click2X, click2Y);

    for (int i = 0; i < frames; i++) {
        if (i == 100 && argc > 3) {   // simulate moving the Settings font-size slider
            var->c_appearance.font_size = atoi(argv[3]);
            printf("cp: font_size -> %d (exercises RebuildFonts)\n", var->c_appearance.font_size);
        }
        if (i == 200 && argc > 6 && strcmp(argv[6], "accentprobe") == 0) {
            // paint the stored accent red (but keep live UI accent green) to find the swatch
            var->c_appearance.accent_enabled = false;
            var->c_appearance.accent_color[0] = 1.f; var->c_appearance.accent_color[1] = 0.f;
            var->c_appearance.accent_color[2] = 0.f; var->c_appearance.accent_color[3] = 1.f;
            printf("cp: accent probe -> red\n");
        }
        {   // clear buffer 0 so each frame renders on black (no ghosting across frames)
            ID3D11Texture2D* bb = nullptr;
            if (SUCCEEDED(sc->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&bb)) && bb) {
                ID3D11RenderTargetView* rv = nullptr;
                if (SUCCEEDED(dev->CreateRenderTargetView(bb, nullptr, &rv))) {
                    const float cc[4] = { 0.f, 0.f, 0.f, 1.f };
                    ctx->ClearRenderTargetView(rv, cc);
                    rv->Release();
                }
                bb->Release();
            }
        }
        if (clickX >= 0 && i == 150) {
            ImGui::GetIO().AddMousePosEvent((float)clickX, (float)clickY);
            ImGui::GetIO().AddMouseButtonEvent(0, true);
            printf("cp: click injected at frame %d\n", i);
        }
        if (clickX >= 0 && i == 151)
            ImGui::GetIO().AddMouseButtonEvent(0, false);
        if (click2X >= 0 && i == 250) {
            ImGui::GetIO().AddMousePosEvent((float)click2X, (float)click2Y);
            ImGui::GetIO().AddMouseButtonEvent(0, true);
            printf("cp: click2 injected at frame %d\n", i);
        }
        if (click2X >= 0 && i == 251)
            ImGui::GetIO().AddMouseButtonEvent(0, false);
        Overlay_RenderFrame(sc);   // drive the render directly (no Present hook here)
        if (i == frames - 1) {   // final frame: after backend lazily built device objects + atlas
            ImFontAtlas* atlas = ImGui::GetIO().Fonts;
            printf("atlas: %dx%d tex=%s fonts=%d\n",
                atlas->TexWidth, atlas->TexHeight,
                atlas->TexPixelsAlpha8 ? "YES" : "NO", (int)atlas->Fonts.size());
            for (int j = 0; j < 2; j++) {
                ImFont* f = set->c_font.inter_medium[j];
                printf("inter_medium[%d]: %s glyphs=%u size=%.1f\n", j,
                    f && f->IsLoaded() ? "loaded" : "BROKEN",
                    f ? (unsigned)f->Glyphs.size() : 0u, f ? f->FontSize : 0.f);
            }
            for (int j = 0; j < 7; j++) {
                ImFont* f = set->c_font.icon[j];
                printf("icon[%d]: %s glyphs=%u size=%.1f\n", j,
                    f && f->IsLoaded() ? "loaded" : "BROKEN",
                    f ? (unsigned)f->Glyphs.size() : 0u, f ? f->FontSize : 0.f);
            }
            // widget/tab glyph probe — 'G' = dropdown arrow, K/F/H/V/E = tab icons, 'D' = tool button
            for (const char* pc = "GKFHVE"; *pc; pc++) {
                const ImFontGlyph* g0 = set->c_font.icon[0]->FindGlyph(*pc);
                printf("icon[0] glyph '%c': %s\n", *pc,
                    g0 && g0->Codepoint == (unsigned)*pc ? "YES" : "MISSING");
            }
            {
                const ImFontGlyph* g4 = set->c_font.icon[4]->FindGlyph('D');
                printf("icon[4] glyph 'D': %s\n",
                    g4 && g4->Codepoint == 'D' ? "YES" : "MISSING");
            }
        }
        if (i < frames - 1) sc->Present(0, 0);   // keep last backbuffer intact for readback
    }
    // Read back BOTH backbuffers (DISCARD model rotates them; the last-rendered
    // frame may live in index 0 or 1).
    for (UINT bi = 0; bi < 2; bi++) {
        ID3D11Texture2D* bb = nullptr;
        if (FAILED(sc->GetBuffer(bi, __uuidof(ID3D11Texture2D), (void**)&bb))) { printf("GetBuffer(%u) failed\n", bi); continue; }

        D3D11_TEXTURE2D_DESC desc; bb->GetDesc(&desc);
        D3D11_TEXTURE2D_DESC st = desc;
        st.Usage = D3D11_USAGE_STAGING;
        st.BindFlags = 0;
        st.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
        st.MiscFlags = 0;
        ID3D11Texture2D* staging = nullptr;
        if (FAILED(dev->CreateTexture2D(&st, nullptr, &staging))) { printf("staging failed\n"); bb->Release(); continue; }
        ctx->CopyResource(staging, bb);

        D3D11_MAPPED_SUBRESOURCE map = {};
        if (FAILED(ctx->Map(staging, 0, D3D11_MAP_READ, 0, &map))) { printf("map failed\n"); staging->Release(); bb->Release(); continue; }
        char path[512]; snprintf(path, sizeof(path), "%s.b%u.bmp", out, bi);
        const bool ok = write_bmp24(path, (const uint8_t*)map.pData, (int)desc.Width, (int)desc.Height);
        ctx->Unmap(staging, 0);
        staging->Release();
        bb->Release();
        printf("buffer %u: %s\n", bi, ok ? path : "WRITE FAILED");
    }

    printf("DONE %d frames\n", frames);
    return 0;
}
