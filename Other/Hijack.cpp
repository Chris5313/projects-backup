bool hijack_overlay() {
	const int maxRetries = 35;
	const DWORD retryDelayMs = 500;

	DirectX9Interface::my_wnd = LI_FN(FindWindowA)(_x("Chrome_WidgetWin_1"), _x("Discord Overlay"));

	if (DirectX9Interface::my_wnd) {
		LI_FN(ShowWindow)(DirectX9Interface::my_wnd, SW_SHOW);
		LI_FN(UpdateWindow)(DirectX9Interface::my_wnd);
		if (settings::visuals::streamproof) {
			typedef BOOL(WINAPI* SetWindowDisplayAffinity_t)(HWND, DWORD);
			HMODULE hUser32 = GetModuleHandleA("user32.dll");
			if (hUser32) {
				SetWindowDisplayAffinity_t SetWindowDisplayAffinity =
					(SetWindowDisplayAffinity_t)GetProcAddress(hUser32, "SetWindowDisplayAffinity");
				if (SetWindowDisplayAffinity) {
					SetWindowDisplayAffinity(DirectX9Interface::my_wnd, 0x11); // WDA_EXCLUDEFROMCAPTURE
				}
			}
		}
		return true;
	}

	LI_FN(MessageBoxA)(
		LI_FN(GetForegroundWindow)(),
		_x("Please Launch Discord Overlay Press Okay To Retry."),
		_x("kernel"),
		MB_OK
		);

	for (int attempt = 1; attempt <= maxRetries; ++attempt) {
		DirectX9Interface::my_wnd = LI_FN(FindWindowA)(_x("Chrome_WidgetWin_1"), _x("Discord Overlay"));
		if (DirectX9Interface::my_wnd) {
			LI_FN(ShowWindow)(DirectX9Interface::my_wnd, SW_SHOW);
			LI_FN(UpdateWindow)(DirectX9Interface::my_wnd);
			if (settings::visuals::streamproof) {
				typedef BOOL(WINAPI* SetWindowDisplayAffinity_t)(HWND, DWORD);
				HMODULE hUser32 = GetModuleHandleA("user32.dll");
				if (hUser32) {
					SetWindowDisplayAffinity_t SetWindowDisplayAffinity =
						(SetWindowDisplayAffinity_t)GetProcAddress(hUser32, "SetWindowDisplayAffinity");
					if (SetWindowDisplayAffinity) {
						SetWindowDisplayAffinity(DirectX9Interface::my_wnd, 0x11); // WDA_EXCLUDEFROMCAPTURE
					}
				}
			}
			return true;
		}

		Sleep(retryDelayMs);
	}


	return false;
}
