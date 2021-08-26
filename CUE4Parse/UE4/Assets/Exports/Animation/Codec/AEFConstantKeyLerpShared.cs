using System;
using System.Collections.Generic;
using System.Text;

namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    class AEFConstantKeyLerpShared : AnimEncodingLegacyBase
    {
        protected readonly AnimationCompressionFormat _format;

        protected AEFConstantKeyLerpShared(AnimationCompressionFormat format) => _format = format;
    }
}