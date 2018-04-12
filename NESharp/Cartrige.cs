using System.IO;
using System;

namespace NESharp
{
    internal class Cartridge
    {
        private const int HeaderMagic = 0x1A53454E;
        private byte[] pgrROM;
        private byte[] chrROM;
        private byte[] pgrRAM;

        //How many PGR ROM has banks 16kB
        public int pgrRomBanks;

        //How many CHR ROM has banks 8kB
        private int chrRomBanks;

        private bool useChrRAM;

        private int flag6;
        private int flag7;

        public bool verticalVRAMMirroring;

        //If true cartridge contains a 512 byte trainer
        public bool containsTrainers;

        public int mapperNumber;

        public Cartridge(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);
            ParseHeader(reader);
            LoadPGRROM(reader);
            LoadCHRROM(reader);

            pgrRAM = new byte[8192];
        }

        private void ParseHeader(BinaryReader reader)
        {
            uint magicNumber = reader.ReadUInt32();
            if (HeaderMagic != magicNumber)
            {
                System.Console.WriteLine("Magic number is wrong");
                return;
            }

            pgrRomBanks = reader.ReadByte();

            chrRomBanks = reader.ReadByte();
            //what if cartridge use chrc ram
            if (chrRomBanks == 0)
            {
                chrRomBanks = 2;
                useChrRAM = true;
            }
            else
            {
                useChrRAM = false;
            }

            // Flags 6
            // 76543210
            // ||||||||
            // |||||||+-Mirroring: 0: horizontal(vertical arrangement)(CIRAM A10 = PPU A11)
            // |||||||             1: vertical(horizontal arrangement)(CIRAM A10 = PPU A10)
            // ||||||+--1: Cartridge contains battery - backed PRG RAM($6000 - 7FFF) or other persistent memory
            // |||||+---1: 512 - byte trainer at $7000 -$71FF(stored before PRG data)
            // ||||+----1: Ignore mirroring control or above mirroring bit; instead provide four - screen VRAM
            // ++++---- - Lower nybble of mapper number
            flag6 = reader.ReadByte();
            verticalVRAMMirroring = (flag6 & 0x01) != 0;
            containsTrainers = (flag6 & 0x04) != 0;

            //TODO: Finish flags 6

            // Flags 7
            // 76543210
            // ||||||||
            // |||||||+-VS Unisystem
            // ||||||+--PlayChoice - 10(8KB of Hint Screen data stored after CHR data)
            // ||||++---If equal to 2, flags 8 - 15 are in NES 2.0 format
            // ++++---- - Upper nybble of mapper number
            flag7 = reader.ReadByte();

            mapperNumber = flag7 & 0xF0 | (flag6 >> 4 & 0xF);

            if ((flag7 & 0x0b0000_1100) == 0x0b0000_1100)
            {
                System.Console.WriteLine("This cartridge is in NES 2.0 format");
            }
        }

        private void LoadPGRROM(BinaryReader reader)
        {
            int pgrRomOffset = containsTrainers ? 16 + 512 : 16;
            reader.BaseStream.Seek(pgrRomOffset, SeekOrigin.Begin);
            pgrROM = new byte[pgrRomBanks * 16383];
            reader.Read(pgrROM, 0, pgrRomBanks * 16383);
        }

        private void LoadCHRROM(BinaryReader reader)
        {
            if (useChrRAM)
            {
                chrROM = new byte[8192];
            }
            else
            {
                chrROM = new byte[chrRomBanks * 8192];
                reader.Read(chrROM, 0, chrRomBanks * 8192);
            }
        }

        public byte ReadPGRROM(int index)
        {
            return pgrROM[index];
        }

        public byte ReadPGRRAM(int index)
        {
            return pgrRAM[index];
        }

        public void WritePGRRAM(int index, byte valume)
        {
            pgrRAM[index] = valume;
        }

        public byte ReadCHR(int index)
        {
            return chrROM[index];
        }

        public void WriteCHR(int index, byte valume)
        {
            if (useChrRAM) return;
            chrROM[index] = valume;
        }
    }
}