# Architecture

TesseractRuntime deliberately separates managed policy from native distribution.

```text
ImageSharp / SkiaSharp / raw pixels
                 |
              OcrImage
                 |
       pool and batch policies
                 |
             OcrEngine
                 |
       stable Tesseract C API
                 |
  RID-specific native dependency closure
```

## Managed boundary

The interop layer binds only stable `capi.h` entry points. It never marshals a C++ class or depends
on compiler-specific layouts. `SafeHandle` owns each `TessBaseAPI` pointer, native strings are
copied to managed UTF-8 strings and released immediately, and pixel memory remains pinned through
the complete recognition pass.

Image buffers are copied on construction. Batch APIs therefore cannot outlive a borrowed span or
race with a caller mutating pixels. Adapters normalize to Gray8 or RGBA32 before entering the core.

## Concurrency

One engine processes one image at a time. `OcrEngine` has its own gate for accidental concurrent
use, while `OcrEnginePool` provides the intended high-throughput model. The pool is bounded and
does not create engines reactively under load. Disposal stops new rentals and waits until every
lease returns.

Batch processing uses indexed result slots, preserving order without serializing native work.
Failures are values associated with individual items. Process cancellation remains exceptional.

## Native boundary

The runtime uses a per-assembly `DllImportResolver`. An explicit full path is preferred so the
actual binary can be reported. Windows uses `LoadLibraryEx` with DLL-load-directory semantics;
Linux and macOS packages use loader-relative RPATH/install names.

The ABI check accepts Tesseract 5.x while the maintained package matrix pins 5.5.2. This lets
operators use a compatible system installation without pretending arbitrary future major versions
are safe.
