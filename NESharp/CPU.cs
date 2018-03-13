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
        ushort PC, SP; //PC - program counter, SP - stack pointer
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
            SP -= 3;
            flagInterrupt = true;
            memory.WriteByte(0x4015, 0);

        }
        void negzero(byte value)
        {
            flagNegative = !(value >= 0x0 && value <= 0x7F);
            flagZero = value == 0x00;
        }
        #region INSTRUCTION OPERATION

        void SET_ZERO(int x)
        {
            flagZero = x == 0;
        }

        void SET_SIGN(int x)
        {
            flagNegative = !(x >= 0x0 && x <= 0x7F);
        }

        void SET_OVERFLOW(bool x)
        {
            flagOverflow = x == true;
        }

        void SET_CARRY(bool x)
        {
            flagCarry = x == true;
        }

#endregion


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
            XR = (byte)SP;
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
            SP = XR;
        }
        //TYA (Transfer Y Index to Accumulator)
        void TYA()
        {
            negzero(YR);
            AC = YR;
        }
        #endregion

        #region Math

        //ADC   Add Memory to Accumulator with Carry
        void ADC (ushort address)
        {
            int src = memory.ReadByte(address);
            int temp = src + AC + (flagCarry ? 1 : 0);
            SET_ZERO(temp & 0xFF);
            if (flagDecimal)
            {
                if (((AC & 0xF) + (src & 0xF) + (flagCarry ? 1 : 0)) > 9)
                    temp += 6;
                SET_SIGN(temp);
                SET_OVERFLOW(!bool.Parse(((AC ^ src) & 0x80).ToString()) && bool.Parse(((AC ^ temp) & 0x80).ToString()));
                if (temp > 0x99)
                { 
                temp += 96;
                SET_CARRY(temp > 0x99);
                }
            }
            else
            {
                SET_SIGN(temp);
                SET_OVERFLOW(!bool.Parse(((AC ^ src) & 0x80).ToString()) && bool.Parse(((AC ^ temp) & 0x80).ToString()));
                SET_CARRY(temp > 0xff);
            }
            AC = (byte.Parse(temp.ToString()));
        }
        //DEC   Decrement Memory by One
        void DEC(ushort address)
        {
            int src = memory.ReadByte(address);
            src = (src - 1) & 0xff;
            SET_SIGN(src);
            SET_ZERO(src);
            memory.WriteByte(address, byte.Parse(src.ToString()));
            
        }
        //DEX   Decrement Index X by One
        void DEX(ushort address)
        {
            int src = XR;
            src = (src - 1) & 0xff;
            SET_SIGN(src);
            SET_ZERO(src);
            XR = (byte.Parse(src.ToString()));
        }
        //DEY   Decrement Index Y by One
        void DEY(ushort address)
        {
            int src = YR;
            src = (src - 1) & 0xff;
            SET_SIGN(src);
            SET_ZERO(src);
            YR = (byte.Parse(src.ToString()));
        }
        //INC   Increment Memory by One
        void INC(ushort address)
        {
            int src = memory.ReadByte(address);
            src = (src + 1) & 0xff;
            SET_SIGN(src);
            SET_ZERO(src);
            memory.WriteByte(address, byte.Parse(src.ToString()));
        }
        //INX   Increment Index X by One
        void INX(ushort address)
        {
            int src = XR;
            src = (src + 1) & 0xff;
            SET_SIGN(src);
            SET_ZERO(src);
            XR = (byte.Parse(src.ToString()));
        }
        //INY   Increment Index Y by One
        void INY(ushort address)
        {
            int src = YR;
            src = (src + 1) & 0xff;
            SET_SIGN(src);
            SET_ZERO(src);
            YR = (byte.Parse(src.ToString()));
        }
        //SBC   Subtract Memory from Accumulator with Borrow  
        void SBC(ushort address)
        {
            int src = memory.ReadByte(address);
            int temp = AC - src - (flagCarry ? 0 : 1);
            SET_SIGN(temp);
            SET_ZERO(temp & 0xff);  /* Sign and Zero are invalid in decimal mode */
            SET_OVERFLOW(bool.Parse(((AC ^ temp) & 0x80).ToString()) && bool.Parse(((AC ^ src) & 0x80).ToString()));
            if (flagDecimal)
            {
                if (((AC & 0xf) - (flagCarry ? 0 : 1)) < (src & 0xf)) /* EP */ temp -= 6;
                if (temp > 0x99) temp -= 0x60;
            }
            SET_CARRY(temp < 0x100);
            AC = byte.Parse((temp & 0xff).ToString());
        }
        #endregion
    }


}
