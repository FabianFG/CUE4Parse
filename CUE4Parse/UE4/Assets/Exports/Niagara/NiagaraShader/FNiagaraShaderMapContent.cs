﻿using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Niagara.NiagaraShader;

public class FNiagaraShaderMapContent : FShaderMapContent
{
    public string FriendlyName;
    public string DebugDescription;
    public FNiagaraShaderMapId ShaderMapId;

    public override void Deserialize(FMemoryImageArchive Ar)
    {
        base.Deserialize(Ar);

        if (Ar.Game >= EGame.GAME_UE5_4)
        {
            ShaderMapId = new FNiagaraShaderMapId(Ar);
        }
        else
        {
            FriendlyName = Ar.ReadFString();
            DebugDescription = Ar.ReadFString();
            ShaderMapId = new FNiagaraShaderMapId(Ar);
            // empty struct NiagaraCompilationOutput = new FNiagaraComputeShaderCompilationOutput(Ar);
        }
    }
}
