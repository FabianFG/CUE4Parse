static_assert(WITH_Oodle, "Missing Oodle");

#include "oodlebasetypes.h"
#include "oodlelzpub.h"
#include "Framework.h"


DLLEXPORT intptr_t OodleLZ_DecompressWrapper(void*                           compBuf,
                                             int						     compBufferSize,
                                             void*                           rawBuf,
                                             int							 rawLen,
                                             oo2::OodleLZ_FuzzSafe			 fuzzSafe          OODEFAULT(oo2::OodleLZ_FuzzSafe_Yes),
                                             oo2::OodleLZ_CheckCRC			 checkCRC          OODEFAULT(oo2::OodleLZ_CheckCRC_No),
                                             oo2::OodleLZ_Verbosity			 verbosity         OODEFAULT(oo2::OodleLZ_Verbosity_None),
                                             void*                           decBufBase        OODEFAULT(NULL),
                                             OO_SINTa						 decBufSize        OODEFAULT(0),
                                             oo2::OodleDecompressCallback*   fpCallback        OODEFAULT(NULL),
                                             void*                           callbackUserData  OODEFAULT(NULL),
                                             void*                           decoderMemory     OODEFAULT(NULL),
                                             OO_SINTa                        decoderMemorySize OODEFAULT(0),
                                             oo2::OodleLZ_Decode_ThreadPhase threadPhase OODEFAULT(oo2::OodleLZ_Decode_Unthreaded)) {
	return oo2::OodleLZ_Decompress(compBuf, compBufferSize, rawBuf, rawLen, fuzzSafe, checkCRC, verbosity, decBufBase, decBufSize, fpCallback, callbackUserData, decoderMemory, decoderMemorySize, threadPhase);
}