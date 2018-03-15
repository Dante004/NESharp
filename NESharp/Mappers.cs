using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESharp
{

    enum VramMirroring
    {
        Horizontal,
        Vertical,
        SingleLower,
        SingleUpper
    }

    abstract class Mappers
    {
        VramMirroring vramMirroring;

        public int VRamAddressToIndex(ushort address)
        {
            int index = (address - 0x2000) % 0x1000;
            switch(vramMirroring)
            {
                case VramMirroring.Vertical:
                    {
                        if (index >= 0x800) index -= 0x800;
                        break;
                    }
                case VramMirroring.Horizontal:
                    {
                        if (index > 0x800) index = ((index - 0x800) % 0x400) + 0x400;
                        else index %= 0x400;
                        break;
                    }
                case VramMirroring.SingleLower:
                    {
                        index %= 0x400;
                        break;
                    }
                case VramMirroring.SingleUpper:
                    {
                        index = (index % 0x400) + 0x400;
                        break;
                    }
            }
            return index;
        }

        abstract public byte ReadByte(ushort address);
        abstract public byte WriteByte(ushort address, byte valume);
    }

    class NROM : Mappers
    {


        public override byte ReadByte(ushort address)
        {
            throw new NotImplementedException();
        }

        public override byte WriteByte(ushort address, byte valume)
        {
            throw new NotImplementedException();
        }
    }


}
