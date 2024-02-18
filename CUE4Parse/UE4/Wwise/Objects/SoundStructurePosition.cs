using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class SoundStructurePosition
    {
        public readonly bool PositionIncluded;
        public readonly EPositionDimensionType? PositionDimension;
        public readonly bool? Position2DPanner;
        public readonly EPosition3DSource? Position3DSource;
        public readonly uint? Position3DAttenuationId;
        public readonly bool? Position3DSpatialization;
        public readonly EPosition3DPlayType? Position3DPlayType;
        public readonly bool? Position3DLoop;
        public readonly uint? Position3DTransitionTime;
        public readonly bool? Position3DFollowListenerOrientation;
        public readonly bool? Position3DUpdatePerFrame;

        public SoundStructurePosition(FArchive Ar)
        {
            PositionIncluded = Ar.Read<bool>();
            if (PositionIncluded)
            {
                PositionDimension = Ar.Read<EPositionDimensionType>();
                if (PositionDimension == EPositionDimensionType.TwoD)
                    Position2DPanner = Ar.Read<bool>();
                else if (PositionDimension == EPositionDimensionType.ThreeD)
                {
                    Position3DSource = Ar.Read<EPosition3DSource>();
                    Position3DAttenuationId = Ar.Read<uint>();
                    Position3DSpatialization = Ar.Read<bool>();
                    if (Position3DSource == EPosition3DSource.UserDefined)
                    {
                        Position3DPlayType = Ar.Read<EPosition3DPlayType>();
                        Position3DLoop = Ar.Read<bool>();
                        Position3DTransitionTime = Ar.Read<uint>();
                        Position3DFollowListenerOrientation = Ar.Read<bool>();
                    }
                    else if (Position3DSource == EPosition3DSource.GameDefined)
                        Position3DUpdatePerFrame = Ar.Read<bool>();
                }
            }
        }

        public void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("PositionIncluded");
            writer.WriteValue(PositionIncluded);

            if (PositionIncluded)
            {
                writer.WritePropertyName("PositionDimension");
                writer.WriteValue(PositionDimension);

                if (PositionDimension == EPositionDimensionType.TwoD)
                {
                    writer.WritePropertyName("Position2DPanner");
                    writer.WriteValue(Position2DPanner);
                }
                else if (PositionDimension == EPositionDimensionType.ThreeD)
                {
                    writer.WritePropertyName("Position3DSource");
                    writer.WriteValue(Position3DSource);

                    writer.WritePropertyName("Position3DAttenuationId");
                    writer.WriteValue(Position3DAttenuationId);

                    writer.WritePropertyName("Position3DSpatialization");
                    writer.WriteValue(Position3DSpatialization);

                    if (Position3DSource == EPosition3DSource.UserDefined)
                    {
                        writer.WritePropertyName("Position3DPlayType");
                        writer.WriteValue(Position3DPlayType);

                        writer.WritePropertyName("Position3DLoop");
                        writer.WriteValue(Position3DLoop);

                        writer.WritePropertyName("Position3DTransitionTime");
                        writer.WriteValue(Position3DTransitionTime);

                        writer.WritePropertyName("Position3DFollowListenerOrientation");
                        writer.WriteValue(Position3DFollowListenerOrientation);

                    }
                    else if (Position3DSource == EPosition3DSource.GameDefined)
                    {
                        writer.WritePropertyName("Position3DUpdatePerFrame");
                        writer.WriteValue(Position3DUpdatePerFrame);
                    }
                }
            }

            writer.WriteEndObject();
        }
    }
}
