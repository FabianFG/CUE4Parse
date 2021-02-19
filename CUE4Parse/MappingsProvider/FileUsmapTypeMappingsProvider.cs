using System.IO;
using System.Threading.Tasks;
using CUE4Parse.Utils;

namespace CUE4Parse.MappingsProvider
{
    public sealed class FileUsmapTypeMappingsProvider : UsmapTypeMappingsProvider
    {
        public string usmapFile;

        public FileUsmapTypeMappingsProvider(string usmapFile)
        {
            this.usmapFile = usmapFile;
            Reload();
        }
        
        public override bool Reload()
        {
            AddUsmap(File.ReadAllBytes(usmapFile), "fortnitegame", usmapFile.SubstringAfterLast('/').SubstringAfterLast('\\'));
            return true;
        }

        public override async Task<bool> ReloadAsync()
        {
            AddUsmap(File.ReadAllBytes(usmapFile), "fortnitegame", usmapFile.SubstringAfterLast('/').SubstringAfterLast('\\'));
            return true;
        }
    }
}