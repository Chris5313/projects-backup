"""
Find WorldToScreen function by scanning for characteristic pattern:
1. Matrix multiply (mulss, addss sequences)
2. Perspective divide (divss xmmN, xmmW where W holds w-component)
3. Screen scaling (mulss with screen width/height)
"""
import sys
import struct
import re

def read_file(path):
    with open(path, 'rb') as f:
        return f.read()

def find_pattern(data, pattern, mask=None):
    """Find pattern with optional mask (? for wildcard)"""
    results = []
    pattern_bytes = bytes.fromhex(pattern.replace(' ', '').replace('?', '00'))
    mask_bytes = bytes([0 if c == '?' else 1 for c in pattern.replace(' ', '')])
    
    for i in range(len(data) - len(pattern_bytes)):
        match = True
        for j, (p, m) in enumerate(zip(pattern_bytes, mask_bytes[::2])):
            if m and data[i + j] != p:
                match = False
                break
        if match:
            results.append(i)
    return results

def scan_w2s_candidates(data, base_addr=0x140000000):
    """
    Scan for W2S function candidates.
    
    W2S signature:
    - Reads ViewProj matrix (movups/movaps from ptr+0x390 or ptr+0x3E0)
    - Does 4 dot products (mulss + addss pattern)
    - Divides by w (divss xmm?, xmm?)
    - Multiplies by screen dimensions
    """
    
    candidates = []
    
    # Pattern 1: Look for divss followed by mulss (perspective divide then screen scale)
    # divss xmmN, xmmM = F3 0F 5E (C0-FF for xmm-xmm)
    # mulss xmmN, xmmM = F3 0F 59 (C0-FF for xmm-xmm) or F3 0F 59 (memory operand)
    
    divss_pattern = b'\xF3\x0F\x5E'  # divss
    mulss_pattern = b'\xF3\x0F\x59'  # mulss
    
    # Find all divss instructions
    divss_locs = []
    i = 0
    while i < len(data) - 10:
        if data[i:i+3] == divss_pattern:
            divss_locs.append(i)
        i += 1
    
    print(f"Found {len(divss_locs)} divss instructions")
    
    # For each divss, check if there's a mulss nearby (within 64 bytes after)
    # This pattern indicates perspective divide followed by screen scaling
    for divss_loc in divss_locs:
        # Look for mulss within 64 bytes after divss
        for j in range(divss_loc, min(divss_loc + 64, len(data) - 3)):
            if data[j:j+3] == mulss_pattern:
                # Found divss followed by mulss - potential W2S
                # Now check if there are matrix offset accesses nearby
                
                # Look backwards for function prologue or large offset access
                func_start = divss_loc
                for k in range(divss_loc, max(0, divss_loc - 500), -1):
                    # Look for push rbp or sub rsp,
                    if data[k:k+1] == b'\x55' or (data[k:k+4] == b'\x48\x83\xEC'):
                        func_start = k
                        break
                    # Or look for a previous ret
                    if data[k:k+1] == b'\xC3':
                        func_start = k + 1
                        break
                
                # Check function size
                func_size = divss_loc - func_start
                if 50 < func_size < 2000:  # Reasonable function size for W2S
                    # Look for offset 0x390 or 0x3E0 access in this function
                    func_data = data[func_start:divss_loc + 100]
                    
                    # Check for 0x390 (little endian: 90 03 00 00)
                    has_390 = b'\x90\x03\x00\x00' in func_data
                    # Check for 0x3E0 (little endian: E0 03 00 00)  
                    has_3e0 = b'\xE0\x03\x00\x00' in func_data
                    # Check for 0x320 (little endian: 20 03 00 00)
                    has_320 = b'\x20\x03\x00\x00' in func_data
                    
                    if has_390 or has_3e0 or has_320:
                        addr = base_addr + func_start
                        candidates.append({
                            'addr': addr,
                            'divss_offset': divss_loc - func_start,
                            'has_390': has_390,
                            'has_3e0': has_3e0,
                            'has_320': has_320,
                            'func_size': func_size
                        })
                break  # Found mulss, move to next divss
    
    return candidates

def main():
    if len(sys.argv) < 2:
        print("Usage: find_w2s.py <memory_dump.bin>")
        print("Scans for WorldToScreen function candidates")
        return
    
    dump_path = sys.argv[1]
    print(f"Loading {dump_path}...")
    data = read_file(dump_path)
    print(f"Loaded {len(data):,} bytes")
    
    print("\nScanning for W2S candidates...")
    candidates = scan_w2s_candidates(data)
    
    print(f"\nFound {len(candidates)} W2S candidates:")
    for i, c in enumerate(candidates[:20]):  # Show top 20
        print(f"  {i+1}. 0x{c['addr']:X} - size={c['func_size']} "
              f"divss@+{c['divss_offset']} "
              f"{'0x390 ' if c['has_390'] else ''}"
              f"{'0x3E0 ' if c['has_3e0'] else ''}"
              f"{'0x320 ' if c['has_320'] else ''}")

if __name__ == '__main__':
    main()
