using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESharp
{
    internal enum VramMirroring
    {
        Horizontal,
        Vertical,
        SingleLower,
        SingleUpper
    }

    internal abstract class Mappers
    {
        protected VramMirroring vramMirroring;

        protected Console console;

        public int VRamAddressToIndex(ushort address)
        {
            int index = (address - 0x2000) % 0x1000;
            switch (vramMirroring)
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

        abstract public void WriteByte(ushort address, byte valume);
    }

    internal class NROM : Mappers
    {
        public NROM(Console console)
        {
            this.console = console;
            vramMirroring = console.cartridge.verticalVRAMMirroring ? VramMirroring.Vertical : VramMirroring.Horizontal;
        }

        private int AddressToIndex(ushort address)
        {
            ushort mappedAddress = (ushort)(address - 0x8000);
            return console.cartridge.pgrRomBanks == 1 ? mappedAddress % 16384 : mappedAddress;
        }

        public override byte ReadByte(ushort address)
        {
            byte data;
            if (address < 0x2000)
            {
                data = console.cartridge.ReadCHR(address);
            }
            else if (address >= 0x8000)
            {
                data = console.cartridge.ReadPGRROM(AddressToIndex(address));
            }
            else
            {
                data = 0;
            }
            return data;
        }

        public override void WriteByte(ushort address, byte valume)
        {
            if (address < 0x2000)
            {
                console.cartridge.WriteCHR(address, valume);
            }
        }
    }
}