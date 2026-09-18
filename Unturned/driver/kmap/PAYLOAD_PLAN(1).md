# Payload Plan — Phase 3

Ponytail rules apply: shortest path that works, boring over clever, one file
until we have a reason for two.

## Ground truth from local BE reversal

Confirmed against `BE-Shellcode/BEShellcode/veh.cpp` + `sigs.cpp` and our
`BEDAISY_DETECTIONS.md`. These are the constraints the payload has to live
inside.

### VEH breakpoint set (BEClient shellcode)

BE finds the FIRST `0xC3` (ret) byte inside each of these functions, writes
`0xCC` (INT3), registers a VEH. When the function returns, the INT3 fires and
the VEH checks the caller's return address on the stack. If the caller is in
non-`MEM_IMAGE` memory with region size > `0x2000`, or the return address
contains a return-spoofing opcode (`FF /2` variants), → **IllegalCaller**
report.

Monitored today (may grow via server-streamed patterns):

- `GetAsyncKeyState`, `NtUserGetAsyncKeyState`
- `GetCursorPos`
- `IsBadReadPtr`
- `GetForegroundWindow`
- `CallWindowProcW`
- `NtUserPeekMessage`
- `NtSetEvent`
- `__stdio_common_vsprintf_s`  ← every printf-family call
- `sqrtf`

**Our payload is `MEM_MAPPED` executable, `SizeOfImage ≈ 0xB000` > `0x2000`.**
Any call to a VEH-instrumented function from our `.text` = detection.

### String signature scan

From `sigs.cpp` — literal byte-string search across every `MEM_MAPPED`/`MEM_PRIVATE`
executable page:

```
ojects\PUBGChinese      BattleGroundsPrivate_CheatESP    Neck         Chest
Mouse 1                 PlayerESPColor                    HackMachine  VisualHacks.net
.rdata$zzzdbg           D3D11Present initialised          [ %.0fM ]    [hp:%d]%dm
d$8                     POSITION                          %s           %d
POSITION                COLOR                             <assembly xmlns='urn:schemas-mi
```

**`%s` and `%d` are on the list.** Any printf-style format string in our binary
is a hit. `\n<assembly xmlns=...` is a match for embedded manifests.

### Kernel-side (BEDaisy) already handled

`BEDAISY_DETECTIONS.md` for the full table. Relevant to payload work:
- No new threads with start address inside our payload (report type 3).
- No callbacks registered from our payload address range (report type 7).

Both apply to us because our payload region is not in `PsLoadedModuleList`.

## Detection surface, translated to code rules

Payload code MUST NOT:

1. **Call any VEH-listed Win32 API directly.** Input via Unity's `Input.*`
   through Mono, not `GetAsyncKeyState`.
2. **Contain `%s`, `%d`, or any of the string signatures.** No `printf`,
   `sprintf`, `snprintf`, `wprintf`, `swprintf`, `vsprintf`, or any variant.
   No `<Format>` embedded manifests. String concatenation by hand or via
   fixed non-format writes.
3. **Create a native thread with start address inside our `.text`.** Any
   worker must have a start address inside a legit module (mono, unity,
   ntdll — anything in `MEM_IMAGE`).
4. **Include the `.rdata$zzzdbg` section marker.** `/DEBUG:NONE` at link.
5. **Register kernel callbacks.** Not applicable — we're user-mode payload.

Payload code MAY:

- Call any Mono API. Mono function calls return to Mono code, not our
  payload — no INT3 triggers on the return.
- Call any Unity method via `mono_runtime_invoke`.
- Read/write memory in the target process freely.
- Read/write files under `%PUBLIC%`.

## Architecture — one file, one job at a time

The payload's job in one sentence: **cross the boundary from native
`payload.dll` into Mono, live inside Unity from then on.**

Everything native we do is bootstrap. Every actual feature lives in Mono land
where the detection surface is thin (Unity itself calls all the same APIs
we'd want, so scanners can't distinguish us).

### Phase 3.0 — Mono handshake (~30 min)

Single goal: prove we can talk to Mono without dying.

```cpp
// payload.cpp — replacing the current 11-line stub

#include <Windows.h>

// One typedef per mono function we need. No abstraction.
using fn_get_root_domain     = void* (*)();
using fn_thread_attach       = void* (*)(void*);
using fn_assembly_open       = void* (*)(void*, const char*);
using fn_assembly_get_image  = void* (*)(void*);
using fn_image_get_name      = const char* (*)(void*);

static void WriteMonoLog(const char* line) {
    HANDLE h = CreateFileW(L"\\\\?\\C:\\Users\\Public\\kmap_mono.txt",
        FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE,
        nullptr, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h == INVALID_HANDLE_VALUE) return;
    SetFilePointer(h, 0, nullptr, FILE_END);
    DWORD n = 0;
    // Manual strlen — no CRT string funcs to keep .text clean of format sigs.
    size_t len = 0; while (line[len]) len++;
    WriteFile(h, line, (DWORD)len, &n, nullptr);
    WriteFile(h, "\r\n", 2, &n, nullptr);
    CloseHandle(h);
}

static void RunOnce() {
    HMODULE mono = GetModuleHandleW(L"mono-2.0-bdwgc.dll");
    if (!mono) { WriteMonoLog("no mono handle"); return; }

    auto get_root_domain    = (fn_get_root_domain)   GetProcAddress(mono, "mono_get_root_domain");
    auto thread_attach      = (fn_thread_attach)     GetProcAddress(mono, "mono_thread_attach");
    auto assembly_open      = (fn_assembly_open)     GetProcAddress(mono, "mono_domain_assembly_open");
    auto assembly_get_image = (fn_assembly_get_image)GetProcAddress(mono, "mono_assembly_get_image");
    auto image_get_name     = (fn_image_get_name)    GetProcAddress(mono, "mono_image_get_name");
    if (!get_root_domain || !thread_attach || !assembly_open ||
        !assembly_get_image || !image_get_name) {
        WriteMonoLog("resolve fail");
        return;
    }

    void* domain = get_root_domain();
    thread_attach(domain);

    void* asm_ = assembly_open(domain, "Assembly-CSharp");
    if (!asm_) { WriteMonoLog("assembly open fail"); return; }

    void* img = assembly_get_image(asm_);
    const char* name = image_get_name(img);
    WriteMonoLog(name ? name : "(null name)");
}

BOOL WINAPI DllMain(HINSTANCE hInst, DWORD reason, LPVOID) {
    if (reason != DLL_PROCESS_ATTACH) return TRUE;
    // Keep the marker — driver still uses it to verify DllMain ran.
    *(volatile unsigned char*)((ULONG_PTR)hInst + 0x100) = 0x42;
    RunOnce();
    return TRUE;
}
```

Expected result: `C:\Users\Public\kmap_mono.txt` contains `Assembly-CSharp`.

Skipped: worker thread, hooks, features. Add when Phase 3.0 confirmed working
on a live server.

### Phase 3.1 — Class + method navigation (~30 min)

Once 3.0 works, extend the same `RunOnce()` to prove we can navigate the type
system. Add:

```
mono_class_from_name(image, "SDG.Unturned", "Provider")
mono_class_get_method_from_name(class, "onServerConnected", 0)
mono_compile_method(method)   // returns native compiled code address
```

Log the resolved native address of one known Unturned method. This proves the
whole path Mono API → managed class → native address works.

Skipped: hooking anything. Just prove we can find the code.

### Phase 3.2 — Persistent execution (~1 hour, THE hard part)

DllMain runs on the hijacked WrQueue thread and returns to ntdll. That thread
goes back to its idle wait. We need code that runs every frame from then on.

Three options, ranked by boringness:

**Option A (best): `mono_thread_create` internal thread.**
Mono exports `mono_thread_create(MonoDomain*, void(*)(void*), void*)`. It
spawns a managed thread whose start address is INSIDE the Mono runtime, not
inside our payload. BE's `PsSetCreateThreadNotifyRoutine` sees a
Mono-owned thread start → not flagged (start addr is in `mono-2.0-bdwgc.dll`,
a legit `MEM_IMAGE` module).

Worker thread body:
```
mono_thread_attach(get_root_domain());
for (;;) {
    do_work();
    Sleep(16);   // ~60 Hz
}
```

**Option B (fallback): trampoline patch on an Unturned method.**
Pick a per-frame method (e.g. `SDG.Unturned.Player.simulate()` at ~50 Hz on
each replicated player, or `PlayerUI.Update()`), grab its native address via
`mono_compile_method`, overwrite the prologue with a 12-byte `jmp qword ptr
[rip+2]; dq target` to our hook function. Hook does work, restores original
bytes on first call, calls original, re-patches. Works but is a real code
patch — every rebuild of Unturned resets the method address and needs a
re-scan.

**Option C (worst): `CreateThread` from payload.**
Native worker thread with start address inside our `.text`. Report type 3
territory. Many published cheats do it and survive; may be fine in Unturned
specifically. Only use if A and B both fail.

Plan: **try A first.** If `mono_thread_create` isn't exported by this
Unity's Mono, fall to B. Only touch C after we know both A and B don't work.

Skipped: any feature. Phase 3.2 just proves we have a heartbeat.

### Phase 3.3 — First read of game state (~30 min)

Inside the persistent worker, once per second, read one thing from Unturned
and log it. Boring choice: `SDG.Unturned.Provider.clients.Count` (number of
connected players). Bumps every time someone connects.

Proves:
- We can hold a class handle across ticks.
- We can read a static field.
- We can survive polling every second on a live server without triggering
  anything.

Skipped: player positions, health, drawing, features.

## Phase 4 — dnSpyEx research (you drive, I consume)

After 3.3 confirmed on server. For each feature you want (see below), we
need from dnSpyEx:

| what | why |
|------|-----|
| Full name of the class (`SDG.Unturned.X`) | For `mono_class_from_name` |
| Field names + types + offsets | Read/write game state |
| Method names + signatures | For lookups + hooks |
| Which method ticks every frame per instance | To know what to hook |
| IL body of `Update`, `LateUpdate`, `simulate` for the relevant classes | Sanity check |

Common feature → class mapping (my guess, dnSpyEx will confirm):

| feature | needs classes |
|---------|---------------|
| Player ESP (box, name, distance, health) | `Provider`, `SteamPlayer`, `Player`, `PlayerLife`, `PlayerMovement`, `MainCamera` |
| Zombie ESP | `ZombieManager`, `Zombie` |
| Item / loot ESP | `ItemManager`, `ItemDrop`, `ItemAsset` |
| Vehicle ESP | `VehicleManager`, `InteractableVehicle` |
| Aimbot | `Player.look`, `PlayerLook.pitch/yaw`, `MainCamera.transform` |
| No recoil | `PlayerEquipment.useable`, `UseableGun.recoil_x/y` |
| Speedhack | `PlayerMovement.pluginSpeedMultiplier` |
| Fly / noclip | `PlayerMovement.gravity`, `PlayerMovement.pluginJumpMultiplier`, or bypass ground check |

## Two decisions still pending from you

1. **Feature priority order.** So I know which classes to reverse first with
   your dnSpyEx runs. My default recommendation: ESP first (visual proof
   things work), aimbot second (highest value), everything else after.

2. **Menu form later.** Confirmed direction: in-Mono UI via Unity's own
   IMGUI/`OnGUI`. Keys via Unity's `Input.GetKey`. No D3D hook, no
   WndProc subclass, no VEH-tripped Win32 input calls. Timing: after
   Phase 3.3 lands. Detail lives in Phase 6 of the earlier roadmap.

## What we're deliberately not doing

- No D3D11 SwapChain hook. VMT hook + IllegalCaller VEH surface for zero gain
  vs Unity's own UI.
- No shared memory + external GUI. Two processes to maintain, no benefit
  over in-Mono UI.
- No IL patching. Requires an IL builder in the payload; trampoline patching
  of the JIT-compiled native code is a lot cheaper.
- No `mono_add_internal_call` + custom C# glue. Would be cleaner in theory
  but requires us to build and load an assembly from scratch. Overkill for
  what a direct hook accomplishes.
- No fancy logging framework. `CreateFileW` + `WriteFile` + hand-formed
  strings, that's it.
- No error-recovery ladders. If Mono resolve fails, write a line to the log
  and return. First working version handles the happy path only.

## Files that will change

- `payload/payload.cpp` — rewritten from 11-line stub. Ends Phase 3.3 at
  maybe 250 lines total.
- `payload/payload.vcxproj` — verify `/DEBUG:NONE`, `/GS-`, static CRT,
  linker `/OPT:REF /OPT:ICF /INCREMENTAL:NO`, no manifest. Kill any debug
  section artifacts so `.rdata$zzzdbg` doesn't sneak in.

Driver and loader don't change in Phase 3. If Phase 3.2 needs kernel-side
help, we'll know when we get there.

## Definition of done for Phase 3

- `kmap_mono.txt` contains `Assembly-CSharp` after each injection.
- One resolved method's native address logged.
- Worker thread ticks in Unturned for at least 20 min without a kick.
- Player count logged and matches server population.

Then Phase 4 begins.
