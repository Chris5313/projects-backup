#pragma once
// In-process memory dumper - runs inside EAC
namespace dumper {
    // Dump game image + heap + structures to C:\Users\Public\
    // Call once after injection is stable (e.g., after first Present)
    void DumpAll();
    
    // Check if dump already exists (skip if so)
    bool DumpExists();
}
