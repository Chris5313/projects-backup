// bmp_analyze.cpp — color histogram + region stats for a 24-bit BMP.
// usage:
//   bmp_analyze file.bmp                 — full-image analysis
//   bmp_analyze file.bmp x y w h         — crop analysis
//   bmp_analyze file.bmp rows x y w h    — per-row light-pixel profile
#include <cstdio>
#include <cstdlib>
#include <cstdint>
#include <cstring>
#include <map>
#include <vector>
#include <algorithm>

struct Px { uint8_t b, g, r; };

int main(int argc, char** argv)
{
    if (argc < 2) { printf("usage: bmp_analyze file.bmp [x y w h | rows x y w h]\n"); return 1; }

    FILE* f = fopen(argv[1], "rb");
    if (!f) { printf("open failed\n"); return 1; }
    uint8_t hdr[54];
    if (fread(hdr, 1, 54, f) != 54) { printf("short header\n"); return 1; }
    int w, h;
    memcpy(&w, hdr + 18, 4);
    memcpy(&h, hdr + 22, 4);
    if (h < 0) h = -h;
    const int row = ((w * 3 + 3) / 4) * 4;
    std::vector<uint8_t> data((size_t)row * h);
    if (fread(data.data(), 1, data.size(), f) != data.size()) { printf("short data\n"); return 1; }
    fclose(f);

    // rows stored bottom-up in file -> flip into image[y][x]
    std::vector<Px> img((size_t)w * h);
    for (int y = 0; y < h; y++) {
        const uint8_t* src = data.data() + (size_t)(h - 1 - y) * row;
        for (int x = 0; x < w; x++) {
            img[(size_t)y * w + x].b = src[x * 3 + 0];
            img[(size_t)y * w + x].g = src[x * 3 + 1];
            img[(size_t)y * w + x].r = src[x * 3 + 2];
        }
    }

    if (argc >= 7 && !strcmp(argv[2], "rows")) {
        const int rx = atoi(argv[3]), ry = atoi(argv[4]), rw = atoi(argv[5]), rh = atoi(argv[6]);
        printf("row profile x[%d..%d) y[%d..%d): light(>100) count per row\n", rx, rx + rw, ry, ry + rh);
        for (int y = ry; y < ry + rh && y < h; y++) {
            int cnt = 0;
            for (int x = rx; x < rx + rw && x < w; x++) {
                const Px& p = img[(size_t)y * w + x];
                if (p.r > 100 || p.g > 100 || p.b > 100) cnt++;
            }
            printf("  y=%4d  %d\n", y, cnt);
        }
        return 0;
    }

    int cx = 0, cy = 0, cw = -1, ch = -1;
    if (argc >= 6) { cx = atoi(argv[2]); cy = atoi(argv[3]); cw = atoi(argv[4]); ch = atoi(argv[5]); }
    const int x0 = (cx >= 0 && cw > 0) ? cx : 0;
    const int y0 = (cy >= 0 && ch > 0) ? cy : 0;
    const int x1 = (cx >= 0 && cw > 0) ? (cx + cw < w ? cx + cw : w) : w;
    const int y1 = (cy >= 0 && ch > 0) ? (cy + ch < h ? cy + ch : h) : h;
    printf("crop: x[%d..%d) y[%d..%d)\n", x0, x1, y0, y1);

    int minX = w, minY = h, maxX = -1, maxY = -1;
    std::map<uint32_t, int> hist;
    long white = 0, textgray = 0, lightgray = 0, accent = 0, anyLight = 0;
    for (int y = y0; y < y1; y++) {
        for (int x = x0; x < x1; x++) {
            const Px& p = img[(size_t)y * w + x];
            const uint32_t key = (p.r << 16) | (p.g << 8) | p.b;
            hist[key]++;
            if (p.r > 235 && p.g > 235 && p.b > 235) white++;
            if (abs(p.r - 80) < 18 && abs(p.g - 90) < 18 && abs((int)p.b - 115) < 22) textgray++;
            if (abs(p.r - 140) < 18 && abs(p.g - 150) < 18 && abs((int)p.b - 175) < 22) lightgray++;
            if (p.g > 120 && p.r < 90 && p.b < 110) accent++;
            if (p.r > 60 || p.g > 60 || p.b > 60) anyLight++;
            if (p.r || p.g || p.b) {
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }
    }
    if (maxX < 0) { printf("crop is entirely black\n"); return 0; }
    printf("non-black bbox in crop: x[%d..%d] y[%d..%d]\n", minX, maxX, minY, maxY);
    printf("unique colors: %zu\n", hist.size());

    std::vector<std::pair<uint32_t, int>> top(hist.begin(), hist.end());
    std::sort(top.begin(), top.end(), [](auto& a, auto& b) { return a.second > b.second; });
    const int ntop = top.size() < 15 ? (int)top.size() : 15;
    printf("top %d colors (count, rgb):\n", ntop);
    for (int i = 0; i < ntop; i++) {
        printf("  %8d  (%3d,%3d,%3d)\n", top[i].second,
            (top[i].first >> 16) & 255, (top[i].first >> 8) & 255, top[i].first & 255);
    }
    printf("signature pixels: white=%ld textgray=%ld hovgray=%ld green-accent=%ld anyLight(>60)=%ld\n",
        white, textgray, lightgray, accent, anyLight);
    return 0;
}
