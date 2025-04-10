using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawRBFBehavior : IRawBase
{
    public RawLODMapping LODSolverMapping;
    public RawRBFSolver[] Solvers;
    public RawRBFPose[] Poses;

    public RawRBFBehavior(FArchiveBigEndian Ar)
    {
        LODSolverMapping = new RawLODMapping(Ar);
        Solvers = Ar.ReadArray(() => new RawRBFSolver(Ar));
        Poses = Ar.ReadArray(() => new RawRBFPose(Ar));
    }
}
