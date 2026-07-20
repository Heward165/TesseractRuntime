# Clean-room record

TesseractRuntime is an independent implementation with a new repository and source history.

Before implementation, the following public behavioral material was reviewed:

- the `charlesw/tesseract` repository description and README;
- public repository counts and release metadata;
- the official Tesseract 5.5.2 release and `capi.h` declarations;
- official Tesseract page-segmentation and OSD documentation;
- official .NET native-library loading documentation;
- public vcpkg Tesseract package metadata.

No source file from the existing .NET wrapper was copied, adapted, or used as an implementation
template. The managed API, resource model, resolver, pooling, batching, adapters, tests, build
system, and documentation were designed for this repository.

The project directly interoperates with Tesseract, whose public C API declarations necessarily
define native function names and signatures. Native packages preserve upstream licenses and
notices.
