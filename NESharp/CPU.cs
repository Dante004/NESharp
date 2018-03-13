using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//												NTSC		PAL
//Frames per second								60			50
//Time per frame (milliseconds)					16.67		20
//Scanlines per frame (of which is V-Blank)		262 (20)	312 (70)
//CPU cycles per scanline						113.33		106.56
//Resolution									256 x 224	256 x 240
//CPU speed										1.79 MHz	1.66 MHz
namespace NESharp
{
    class CPU
    {
        /// Processor Flag
		/// 1) Carry flag        - Set if the last instruction resulted in an over or underflow. Used for arithmetic on numbers larger than one byte, where the next instruction is carry-flag aware.
		/// 2) Zero flag         - Set if the last instruction resulted in a value of 0
		/// 3) Interrupt Disable - Set to disable responding to maskable interrupts
		/// 4) Decimal Mode      - Set to enable BCD mode. This doesn't affect the 2A03 so flipping this value doesn't do anything.
		/// 5) Break Command     - Set to indicate a `BRK` instruction was executed
		/// 6) Unused bit
		/// 7) Overflow flag     - Set when an invalid two's complement number is the result of an operation. An example is adding 2 positive numbers which results in the sign bit being set, making the result a negative.
		/// 8) Negative flag     - Set if the number is negative, determined by checking the sign bit (7th bit)
        /// 
        bool flagCarry, flagZero, flagInterrupt, flagDecimal, flagBreak, flagOverflow, flagNegative;
        byte AC, XR, YR; //AC - accumulator, XR - X register, YR - Y register
        ushort PC, S; //PC - program counter, S - stack pointer
        int cycle;
        public Memory memory;
        public CPU(Memory memory)
        {
            this.memory = memory;
        }


        public void PowerUp()
        {
            PC = 0x34;
            AC = 0x0;
            XR = 0x0;
            YR = 0x0;

            memory.WriteByte(0x4017, 0x00);
            memory.WriteByte(0x4015, 0x00);
            for (ushort i = 0x4000; i<=0x400F; i++)
            {
                memory.WriteByte(i, 0x00);
            }
            //TODO: All 15 bits of noise channel LFSR = $0000[3]. The first time the LFSR is clocked from the all-0s state, it will shift in a 1.
        }
        public void Reset()
        {
            S -= 3;
            flagInterrupt = true;
            memory.WriteByte(0x4015, 0);

        }
        void negzero(byte value)
        {
            flagNegative = !(value >= 0x0 && value <= 0x7F);
            flagZero = value == 0x00;
        }
        #region storage
        //LDA (Load Accumulator With Memory)
        void LDA(ushort address)
        {
            AC = memory.ReadByte(address);
            negzero(AC);
        }
        //LDX (Load X Index With Memory)
        void LDX(ushort address)
        {
            XR = memory.ReadByte(address);
            negzero(XR);
        }
        //LDY (Load Y Index With Memory)
        void LDY(ushort address)
        {
            YR = memory.ReadByte(address);
            negzero(YR);
        }
        //STA (Store Accumulator In Memory)
        void STA(ushort address)
        {
            memory.WriteByte(address, AC);
        }
        //STX (Store X Index In Memory)
        void STX(ushort address)
        {
            memory.WriteByte(address, XR);
        }
        //STY (Store Y Index In Memory)
        void STY(ushort address)
        {
            memory.WriteByte(address, YR);
        }
        //TAX (Transfer Accumulator to X Index)
        void TAX()
        {
            negzero(AC);
            XR = AC;
        }
        //TAY (Transfer Accumulator to Y Index)
        void TAY()
        {
            negzero(AC);
            YR = AC;
        }
        //TSX (Transfer Stack Pointer to X Index)
        void TSX()
        {
            negzero(XR);
            XR = (byte)S;
        }
        //TXA (Transfer X Index to Accumulator)
        void TXA()
        {
            negzero(XR);
            AC = XR;
        }
        //TXS (Transfer X Index to Stack Pointer) 
        void TXS()
        {
            negzero(XR);
            S = XR;
        }
        //TYA (Transfer Y Index to Accumulator)
        void TYA()
        {
            negzero(YR);
            AC = YR;
        }
#endregion
    }


}
