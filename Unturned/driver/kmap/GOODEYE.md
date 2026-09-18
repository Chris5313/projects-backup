# GoodEye — IAT Hooking BEDaisy

Source: https://github.com/huoji120/goodeye

## What It Does

Hooks BEDaisy.sys's Import Address Table (IAT) to intercept key kernel APIs
before BEDaisy can use them for detection. Prevents reports from being generated
at the source.

## Technique

BEDaisy.sys imports from ntoskrnl.exe. GoodEye hooks these imports:

1. **ExAllocatePool / ExAllocatePoolWithTag** — BEDaisy allocates report nodes
   (tag 'EB', 24 bytes PagedPool). Hook to return nullptr → report silently dropped.
   (Same principle as BlindEye but at IAT level rather than pool exhaustion)

2. **ZwQuerySystemInformation** — BEDaisy uses SystemProcessInformation (class 5)
   to enumerate threads/processes. Hook to filter results, hide our threads.

3. **MmGetSystemRoutineAddress** — BEDaisy resolves APIs dynamically. Hook to
   return fake pointers or nullptr for specific functions.

## Why It's Interesting (Reference Only)

- Shows that BEDaisy's own imports aren't integrity-checked against IAT modification
- Demonstrates the ExAllocatePool report suppression concept
- Confirms that report nodes are allocated per-detection, not batched

## Why We DON'T Use This

1. **Requires being loaded BEFORE BEDaisy** — GoodEye must hook the IAT before
   BEDaisy's DriverEntry resolves imports. We load AFTER BEDaisy (via kdmapper
   which runs after BEService starts BEDaisy).

2. **BEDaisy self-validates** — report types 5-8 check driver integrity including
   IAT consistency (periodically hashes own sections).

3. **Fragile** — Any BEDaisy update changes import order or adds integrity checks.

4. **We don't need report suppression** — Our approach is to not trigger reports
   in the first place (fake LDR entry, section-backed mapping, PE header wipe).

## Useful Concepts Borrowed

- Confirms report allocation pattern: `ExAllocatePool(PagedPool, 24)` with tag 'EB'
- Confirms that BEDaisy's system thread is the main scanning loop
- Shows ZwQuerySystemInformation class 5 is the thread enumeration vector
