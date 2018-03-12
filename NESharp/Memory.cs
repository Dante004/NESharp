using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// [0x0000-0x00FF]	- RAM for Zero-Page & Indirect-Memory Addressing
// [0x0100-0x01FF]	- RAM for Stack Space & Absolute Addressing
// [0x0200-0x3FFF]	- RAM for programmer use
// [0x4000-0x7FFF]	- Memory mapped I/O
// [0x8000-0xFFF9]	- ROM for programmer useage
// [0xFFFA]			- Vector address for NMI (low byte)
// [0xFFFB]			- Vector address for NMI (high byte)
// [0xFFFC]			- Vector address for RESET (low byte)
// [0xFFFD]			- Vector address for RESET (high byte)
// [0xFFFE]			- Vector address for IRQ & BRK (low byte)
// [0xFFFF]			- Vector address for IRQ & BRK  (high byte)     

// Address Range  	Size 			Notes (Page size is 256 bytes)
// $0000–$00FF 		256 bytes 		Zero Page — Special Zero Page addressing modes give faster memory read/write access
// $0100–$01FF 		256 bytes 		Stack memory
// $0200–$07FF 		1536 bytes 		RAM
// $0800–$0FFF 		2048 bytes 		Mirror of $0000–$07FF 	$0800–$08FF Zero Page
//															$0900–$09FF	Stack
//															$0A00–$0FFF	RAM
// $1000–$17FF 		2048 bytes 		Mirror of $0000–$07FF 	$1000–$10FF Zero Page
//															$1100–$11FF Stack
//															$1200–$17FF RAM
// $1800–$1FFF 		2048 bytes 		Mirror of $0000–$07FF 	$1800–$18FF Zero Page
//															$1900–$19FF Stack
//															$1A00–$1FFF RAM
// $2000–$2007 		8 bytes 		Input/Output registers
// $2008–$3FFF 		8184 bytes 		Mirror of $2000–$2007 (multiple times)
// $4000–$401F 		32 bytes 		Input/Output registers
// $4020–$5FFF 		8160 bytes 		Expansion ROM — Used with Nintendo's MMC5 to expand the capabilities of VRAM.
// $6000–$7FFF 		8192 bytes 		SRAM — Save Ram used to save data between game plays.
// $8000–$FFFF 		32768 bytes		PRG-ROM
// $FFFA–$FFFB 		2 bytes 		Address of Non Maskable Interrupt (NMI) handler routine
// $FFFC–$FFFD 		2 bytes 		Address of Power on reset handler routine
// $FFFE–$FFFF 		2 bytes 		Address of Break (BRK instruction) handler routine
namespace NESharp
{
    interface ICartige
    {
        byte ReadByte(ushort address);
        void WriteByte(ushort address,byte valume);
    }

    abstract class Memory
    {
        abstract public byte ReadByte(ushort address);
        abstract public void WriteByte(ushort address, byte valume);
    }

    class CPUMemory : Memory
    {
        byte[] zeroPage;
        byte[] stack;
        byte[] ram;
        byte[] IORegister1;
        byte[] IORegister2;
        byte[] sram;
        byte[] PGR_ROM;

        ICartige cartige;

        public CPUMemory(ICartige cartige)
        {
            zeroPage = new byte[256];
            stack = new byte[256];
            ram = new byte[1536];
            IORegister1 = new byte[8];
            IORegister2 = new byte[32];
            sram = new byte[8192];
            PGR_ROM = new byte[32768];
            this.cartige = cartige;
        }

        public override byte ReadByte(ushort address)
        {
            #region RAM
            if (address >= 0x0000 && address <= 0x00FF)
            {
                return zeroPage[address];
            }
            else if (address >= 0x0100 && address <= 0x01FF)
            {
                return stack[address - 0x0100];
            }
            else if (address >= 0x0200 && address <= 0x07FF)
            {
                return ram[address - 0x0200];
            }
            #endregion
            #region Mirror RAM
            else if (address >= 0x0800 && address <= 0x08FF)
            {
                return zeroPage[address - 0x0800];
            }
            else if (address >= 0x0900 && address <= 0x09FF)
            {
                return stack[address - 0x08900];
            }
            else if (address >= 0x0A00 && address <= 0x0FFF)
            {
                return ram[address - 0x0A00];
            }
            else if(address >=0x1000 && address <=0x10FF)
            {
                return zeroPage[address - 0x1000];
            }
            else if (address >= 0x1100 && address <= 0x11FF)
            {
                return stack[address - 0x1100];
            }
            else if (address >= 0x1200 && address <= 0x17FF)
            {
                return ram[address - 0x10FF];
            }
            else if (address >= 0x1800 && address <= 0x18FF)
            {
                return zeroPage[address - 0x1800];
            }
            else if (address >= 0x1900 && address <= 0x19FF)
            {
                return stack[address - 0x1900];
            }
            else if (address >= 0x1A00 && address <= 0x1FFF)
            {
                return ram[address - 0x1A00];
            }
            #endregion
            #region I/O Register 1
            else if (address>= 0x2000 && address <= 0x2007)
            {
                return IORegister1[address - 0x2000];
            }
            #endregion
            #region Mirror I/O Register1
            //TODO: Napisać mirrory do I/O
            #endregion
            #region I/O Register2
            else if (address >= 0x4000 && address <= 0x401F)
            {
                return IORegister1[address - 0x4000];
            }
            #endregion
            #region ROM
            else if (address >= 0x4020 && address <= 0x5FFF)
            {
                return cartige.ReadByte(address);
            }
            #endregion
            #region SRAM
            else if (address >= 0x6000 && address <= 0x7FFF)
            {
                return sram[address - 0x6000];
            }
            #endregion
            #region PGR-ROM
            else if(address>=0x8000 && address<=0xFFFF)
            {
                return PGR_ROM[address - 0x8000];
            }
            #endregion
            else
            {
                throw new Exception("Instruction isn't exist");
            }
        }

        public override void WriteByte(ushort address, byte valume)
        {
            throw new NotImplementedException();
        }
    }
}
