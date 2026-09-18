// texture_loader.h — Pure D3D11 texture loading from memory (replaces D3DX11)
// Uses stb_image for decoding, plain D3D11 API for GPU upload.
#pragma once
#include <d3d11.h>

// Load a texture from compressed image data in memory (PNG/JPG/BMP/GIF).
// Returns an ID3D11ShaderResourceView* in *out_srv. Returns true on success.
bool LoadTextureFromMemory(const unsigned char* data, int data_size,
                           ID3D11Device* device,
                           ID3D11ShaderResourceView** out_srv);
