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
    abstract class Memory
    {
        abstract public byte ReadByte(ushort address);
        abstract public void WriteByte(ushort address, byte valume);
    }

    class CPUMemory:Memory
    {
        byte[] ram;

        public CPUMemory()
        {
            ram = new byte[2048];
        }

        public void Reset()
        {
            Array.Clear(ram, 0, ram.Length);
        }

        public override byte ReadByte(ushort address)
        {
            byte data;
            if(address < 0x2000)
            {
                data = ram[address];
            }
            else
            {
                throw new Exception("Wrong address");
            }
            return data;
        }

        public override void WriteByte(ushort address, byte valume)
        {
            if(address < 0x2000)
            {
                ram[address] = valume;
            }
            else
            {
                throw new Exception("Wrong address");
            }
        }
    }

    class PPUMemory : Memory
    {
        public override byte ReadByte(ushort address)
        {
            throw new NotImplementedException();
        }

        public override void WriteByte(ushort address, byte valume)
        {
            throw new NotImplementedException();
        }
    }
}
