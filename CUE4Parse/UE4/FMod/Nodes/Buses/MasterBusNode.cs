using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes.Buses;

public class MasterBusNode(BinaryReader Ar) : BaseBusNode(Ar, FModReader.Version >= 0x49);
