using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes.Buses;

public class OutputPortNode(BinaryReader Ar) : BaseBusNode(Ar, true);
