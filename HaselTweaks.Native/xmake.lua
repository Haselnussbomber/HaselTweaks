add_rules("mode.debug", "mode.releasedbg")

target("mimalloc")
    set_kind("static")
    add_files(
        "lib/mimalloc/src/alloc.c",
        "lib/mimalloc/src/alloc-aligned.c",
        "lib/mimalloc/src/alloc-posix.c",
        "lib/mimalloc/src/arena.c",
        "lib/mimalloc/src/bitmap.c",
        "lib/mimalloc/src/heap.c",
        "lib/mimalloc/src/init.c",
        "lib/mimalloc/src/libc.c",
        "lib/mimalloc/src/options.c",
        "lib/mimalloc/src/os.c",
        "lib/mimalloc/src/page.c",
        "lib/mimalloc/src/random.c",
        "lib/mimalloc/src/segment.c",
        "lib/mimalloc/src/segment-map.c",
        "lib/mimalloc/src/stats.c",
        "lib/mimalloc/src/prim/prim.c")
    add_includedirs("lib/mimalloc/include")
    add_syslinks("advapi32")

target("libdeflate")
    set_kind("static")
    add_files("lib/libdeflate/lib/*.c", "lib/libdeflate/lib/x86/*.c")

target("HaselTweaks.Native")
    set_kind("shared")
    add_headerfiles("src/*.h")
    set_pcheader("src/pch.h")
    add_files("src/*.cpp")
    add_includedirs("lib/mimalloc/include", "lib/libdeflate")
    add_deps("mimalloc", "libdeflate")
    after_build(function (target)
        os.mkdir("../bin")

        local targetfile = target:targetfile()
        os.cp(targetfile, "../bin")

        local symbolfile = target:symbolfile()
        os.cp(symbolfile, "../bin")
    end)
