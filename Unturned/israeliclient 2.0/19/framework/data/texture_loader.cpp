// texture_loader.cpp — Pure D3D11 texture loading from memory (replaces D3DX11)
// Uses stb_image for decoding, plain D3D11 API for GPU upload.
#define STB_IMAGE_IMPLEMENTATION
#include "../../thirdparty/stb_image.h"
#include "texture_loader.h"
#include <cstring>

bool LoadTextureFromMemory(const unsigned char* data, int data_size,
                           ID3D11Device* device,
                           ID3D11ShaderResourceView** out_srv)
{
    if (!data || !device || !out_srv) return false;

    int w, h, channels;
    unsigned char* pixels = stbi_load_from_memory(data, data_size, &w, &h, &channels, 4);
    if (!pixels) return false;

    D3D11_TEXTURE2D_DESC desc = {};
    desc.Width            = w;
    desc.Height           = h;
    desc.MipLevels        = 1;
    desc.ArraySize        = 1;
    desc.Format           = DXGI_FORMAT_R8G8B8A8_UNORM;
    desc.SampleDesc.Count = 1;
    desc.Usage            = D3D11_USAGE_DEFAULT;
    desc.BindFlags        = D3D11_BIND_SHADER_RESOURCE;

    D3D11_SUBRESOURCE_DATA sub = {};
    sub.pSysMem     = pixels;
    sub.SysMemPitch = w * 4;

    ID3D11Texture2D* tex = nullptr;
    HRESULT hr = device->CreateTexture2D(&desc, &sub, &tex);
    if (FAILED(hr)) {
        stbi_image_free(pixels);
        return false;
    }

    D3D11_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
    srvDesc.Format                    = desc.Format;
    srvDesc.ViewDimension             = D3D11_SRV_DIMENSION_TEXTURE2D;
    srvDesc.Texture2D.MipLevels       = 1;
    srvDesc.Texture2D.MostDetailedMip  = 0;

    hr = device->CreateShaderResourceView(tex, &srvDesc, out_srv);
    tex->Release();
    stbi_image_free(pixels);

    return SUCCEEDED(hr);
}
