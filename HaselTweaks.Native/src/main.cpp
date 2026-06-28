#include <igzip_lib.h>

extern "C" __declspec(dllexport) int __fastcall Inflate(unsigned char* dest, unsigned long* destLen, const unsigned char* source, unsigned long sourceLen) {
    thread_local inflate_state state;
    isal_inflate_init(&state);

    state.next_in = const_cast<unsigned char*>(source);
    state.avail_in = static_cast<uint32_t>(sourceLen);
    state.next_out = dest;
    state.avail_out = static_cast<uint32_t>(*destLen);

    int err = isal_inflate_stateless(&state);
    if (err == ISAL_DECOMP_OK)
        *destLen = static_cast<unsigned long>(state.total_out);

    return err;
}
