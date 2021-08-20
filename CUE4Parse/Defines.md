NAME_HASHES (for serializing and using name hashes in a FNameEntry)

NO_FNAME_VALIDATION (for deactivating validation of FNames on serialization and throwing and exception)

NO_STRING_NULL_TERMINATION_VALIDATION (for deactivating validation of null terminator on strings)

READ_SHADER_MAPS (for enabling deserialization of FMaterialResource which is untested and so far no useful data can be extracted from it)

USE_LZ4_NATIVE_LIB (to use the [native LZ4 library](https://github.com/lz4/lz4) instead of [K4os.Compression.LZ4](https://github.com/MiloszKrajewski/K4os.Compression.LZ4))