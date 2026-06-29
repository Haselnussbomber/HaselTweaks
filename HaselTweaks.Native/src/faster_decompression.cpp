#include "pch.h"
#include "faster_decompression.h"

EXPORT int __fastcall Inflate(unsigned char* dest, size_t* destLen, const unsigned char* source, size_t sourceLen) {
    thread_local std::unique_ptr<libdeflate_decompressor, decltype(&libdeflate_free_decompressor)>
        decompressor(libdeflate_alloc_decompressor(), &libdeflate_free_decompressor);

    if (!decompressor)
        return -1;

    size_t actual_out = 0;

    int result = libdeflate_deflate_decompress(
        decompressor.get(),
        source,
        sourceLen,
        dest,
        *destLen,
        &actual_out
    );

    if (result == LIBDEFLATE_SUCCESS)
        *destLen = actual_out;

    return result;
}
