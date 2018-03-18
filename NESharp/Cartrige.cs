using System.IO;

namespace NESharp
{
    class Cartridge
    {
        const int headerMagic = 0x1A53454E;

        byte[] pgrROM;
        byte[] chr;
        byte[] pgrRAM;
    
        public Cartridge(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);

            pgrRAM = new byte[8192];
        }
    }
}
