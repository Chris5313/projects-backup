#pragma once
void GW2_Framework_Init(struct ID3D11Device* device);
void GW2_Framework_Shutdown();
void GW2_Framework_Render();
void GW2_Framework_RenderWatermark();  // Renders watermark only (always visible)
