#include "pch.h"
#include "faster_decompression.h"

EXPORT void __fastcall ReadSqpkChunk(uintptr_t srcData, uintptr_t dstData) {
    uint8_t* source = *reinterpret_cast<uint8_t**>(srcData + 8);
    uint8_t* dest = *reinterpret_cast<uint8_t**>(dstData + 0x78);

    const auto header = *reinterpret_cast<SqpkBlockHeader*>(source);

    if (header.decompressed_size == 0)
        return;

    if (header.compressed_size == 32000) {
        std::memcpy(dest, source + header.header_size, header.decompressed_size);
    }
    else {
        thread_local std::unique_ptr<libdeflate_decompressor, decltype(&libdeflate_free_decompressor)>
            decompressor(libdeflate_alloc_decompressor(), &libdeflate_free_decompressor);

        libdeflate_deflate_decompress(
            decompressor.get(),
            source + header.header_size,
            header.compressed_size,
            dest,
            header.decompressed_size,
            nullptr);
    }
}
