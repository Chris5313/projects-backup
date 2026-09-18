#pragma once
#include <cstdint>

namespace entfinder {

// Run the full entity list scan (slow - call on F9 or startup)
void FindEntityList();

// Get discovered results
uint64_t GetEntityListBase();
int GetEntityCount();
uint64_t* GetCharObjs();
int GetCharObjCount();

} // namespace entfinder
