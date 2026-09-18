#pragma once
#include "imgui.h"
#include <d3d11.h>

namespace GUI {
    extern ImFont* FontText;
    extern ImFont* FontTitle;
    extern ImFont* FontSmall;
    extern ImFont* FontIcon;

    extern ID3D11ShaderResourceView* GameTexture;
    extern int GameWidth, GameHeight;

    extern ID3D11ShaderResourceView* LogoTexture;
    extern int LogoWidth, LogoHeight;

    extern ID3D11ShaderResourceView* BannerTexture;
    extern int BannerWidth, BannerHeight;

    extern ID3D11ShaderResourceView* FrontTexture;
    extern int FrontWidth, FrontHeight;

    extern ID3D11ShaderResourceView* Gw2Texture;
    extern int Gw2Width, Gw2Height;

    extern bool WantsClose;
    extern bool LoggedIn;
    extern bool NotAdmin;
    extern bool HoveringInteractive;
    extern bool GhostMode;

    void Init(ID3D11Device* device);
    void RenderLogin(float width, float height, float offsetX = 0.0f);
    void RenderDashboard(float width, float height, float offsetX = 0.0f);
    void RenderFrame(float width, float height);
    void Shutdown();
}
