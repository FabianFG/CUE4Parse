#include "Framework.h"
#include <Alembic/AbcCoreOgawa/All.h>
#include <Alembic/AbcGeom/All.h>
#include <cstdint>
#include <span>
#include <sstream>
#include <string>
#include <vector>

namespace Abc = Alembic::Abc;
namespace Geom = Alembic::AbcGeom;

struct GroomPoint
{
    float X;
    float Y;
    float Z;
    float Radius;
};

struct GroomRootUv
{
    float U;
    float V;
};

struct GroomArchive
{
    std::string Name;
    std::ostringstream Stream{std::ios::binary};
    Abc::OArchive Archive{Alembic::AbcCoreOgawa::WriteArchive()(&Stream, Abc::MetaData())};
    Geom::OXform Root;

    // Just in case fallback to "Groom" if the name is empty or null
    explicit GroomArchive(const char* name) : Name(name&&* name ? name : "Groom"), Root(Archive.getTop(), Name)
    {
        Geom::XformSample transform;
        auto& schema = Root.getSchema();
        schema.set(transform);

        const auto attributes = schema.getArbGeomParams();
        const int16_t major = 1;
        const int16_t minor = 5;
        Geom::OInt16GeomParam versionMajor(attributes, "groom_version_major", false, Geom::kConstantScope, 1);
        Geom::OInt16GeomParam versionMinor(attributes, "groom_version_minor", false, Geom::kConstantScope, 1);
        versionMajor.set({Abc::Int16ArraySample(&major, 1), Geom::kConstantScope});
        versionMinor.set({Abc::Int16ArraySample(&minor, 1), Geom::kConstantScope});
    }
};

DLLEXPORT GroomArchive* nCreateAlembicGroom(const char* name) noexcept try
{
    return new GroomArchive(name);
}
catch (...)
{
    return nullptr;
}

DLLEXPORT int nWriteAlembicGroomCurves(GroomArchive* Ar, int32_t groupId, bool isGuide, const int32_t* pointsPerCurve,
    int32_t curveCount, const GroomPoint* points, int32_t pointCount, const GroomRootUv* rootUvs)
{
    try
    {
        const auto groupName = Ar->Name + "_Group_" + std::to_string(groupId) + (isGuide ? "_Guides" : "_Strands");

        Geom::OXform transform(Ar->Root, groupName);
        Geom::XformSample transformSample;
        transform.getSchema().set(transformSample);

        Geom::OCurves curves(transform, "Curves");
        auto& schema = curves.getSchema();

        std::vector<Abc::V3f> positions;
        std::vector<float> widths;
        positions.reserve(pointCount);
        widths.reserve(pointCount);
        for (const auto& point : std::span(points, pointCount))
        {
            positions.emplace_back(point.X, point.Z, point.Y);
            widths.push_back(point.Radius * 2.0f);
        }

        const Geom::OFloatGeomParam::Sample widthSample(Abc::FloatArraySample(widths), Geom::kVertexScope);
        const Abc::Int32ArraySample curveSizes(pointsPerCurve, curveCount);
        Geom::OCurvesSchema::Sample sample(Abc::P3fArraySample(positions), curveSizes, Geom::kLinear, Geom::kNonPeriodic, widthSample);
        sample.setBasis(Geom::kNoBasis);
        schema.set(sample);

        const auto attributes = schema.getArbGeomParams();
        Geom::OInt32GeomParam groupIdParam(attributes, "groom_group_id", false, Geom::kConstantScope, 1);
        groupIdParam.set({Abc::Int32ArraySample(&groupId, 1), Geom::kConstantScope});
        Geom::OInt32GeomParam guideParam(attributes, "groom_guide", false, Geom::kConstantScope, 1);
        const int32_t guideFlag = isGuide;
        guideParam.set({Abc::Int32ArraySample(&guideFlag, 1), Geom::kConstantScope});
        if (rootUvs)
        {
            std::vector<Abc::V2f> uvs;
            uvs.reserve(curveCount);
            for (const auto& uv : std::span(rootUvs, curveCount))
                uvs.emplace_back(uv.U, uv.V);

            Geom::OV2fGeomParam rootUvParam(attributes, "groom_root_uv", false, Geom::kUniformScope, 1);
            rootUvParam.set({Abc::V2fArraySample(uvs), Geom::kUniformScope});
        }
        return 1;
    }
    catch (...)
    {
        return 0;
    }
}

DLLEXPORT const char* nFinishAlembicGroom(GroomArchive* archive, size_t* length)
{
    archive->Root.reset();
    archive->Archive.reset();

    const auto bytes = archive->Stream.view();
    *length = bytes.size();
    return archive->Stream ? bytes.data() : nullptr;
}

DLLEXPORT void nDestroyAlembicGroom(GroomArchive* archive)
{
    delete archive;
}
