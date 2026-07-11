#pragma once

struct SqpkBlockHeader {
    uint32_t header_size;
    uint32_t padding;
    uint32_t compressed_size;
    uint32_t decompressed_size;
};

EXPORT void __fastcall ReadSqpkChunk(uintptr_t srcData, uintptr_t dstData);
