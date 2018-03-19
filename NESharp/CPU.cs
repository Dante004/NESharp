﻿using System;
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
        int clk; //clk : the number of cycles an instruction takes.
        public CPUMemory memory;
        public CPU(CPUMemory memory)
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
            for (ushort i = 0x4000; i <= 0x400F; i++)
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

        void SET_OVERFLOW(int x)
        {
            flagOverflow = x > 255;
        }

        void SET_CARRY(bool x)
        {
            flagCarry = x == true;
        }
        void SET_DECIMAL(bool x)
        {
            flagDecimal = x;
        }
        void SET_INTERRUPT(bool x)
        {
            flagInterrupt = x;
        }
        void SET_BREAK(bool x)
        {
            flagBreak = x;
        }
        bool IF_CARRY()
        {
            return flagCarry;
        }
        bool IF_OVERFLOW()
        {
            return flagOverflow;
        }
        bool IF_SIGN()
        {
            return flagNegative;
        }
        bool IF_ZERO()
        {
            return flagZero;
        }
        bool IF_DECIMAL()
        {
            return flagDecimal;
        }

        //REL_ADDR(PC, src) : returns the relative address obtained by adding the displacement src to the PC.
        ushort REL_ADDR(ushort pc, byte src)
        {
            return (ushort)(pc + src);
        }

        byte PULL()
        {
            SP++;
            byte data = memory.ReadByte((ushort)(0x100 | SP));
            return data;
        }
        void PUSH(byte data)
        {
            memory.WriteByte((ushort)(0x100 | SP), data);
            SP--;
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
        void ADC(ushort address)
        {
            byte src = memory.ReadByte(address);
            byte carry = (byte)(flagCarry ? 1 : 0);
            byte temp = (byte)(src + AC + carry);
            negzero(temp);
            flagOverflow = (~(AC ^ src) & (AC ^ temp) & 0x80) != 0;
            flagCarry = src > 0xFF;
            AC = temp;
        }
        //DEC   Decrement Memory by One
        void DEC(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = (byte)((src - 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            memory.WriteByte(address, src);

        }
        //DEX   Decrement Index X by One
        void DEX(ushort address)
        {
            byte src = XR;
            src = (byte)((src - 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            XR = (src);
        }
        //DEY   Decrement Index Y by One
        void DEY(ushort address)
        {
            byte src = YR;
            src = (byte)((src - 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            YR = (src);
        }
        //INC   Increment Memory by One
        void INC(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = (byte)((src + 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            memory.WriteByte(address, src);
        }
        //INX   Increment Index X by One
        void INX(ushort address)
        {
            byte src = XR;
            src = (byte)((src + 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            XR = (src);
        }
        //INY   Increment Index Y by One
        void INY(ushort address)
        {
            byte src = YR;
            src = (byte)((src + 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            YR = (src);
        }
        //SBC   Subtract Memory from Accumulator with Borrow  
        void SBC(ushort address)
        {
            byte src = memory.ReadByte(address);
            byte carry = (byte)(flagCarry ? 0 : 1);
            byte temp = (byte)(AC - src - carry);
            negzero(temp);
            flagOverflow = (((AC ^ temp) & (AC ^ src)) & 0x80) != 0;
            SET_CARRY(temp < 0x100);
            AC = temp;
            //TODO: spradź czy to działa
        }
        #endregion

        #region Bitwise

        //AND   "AND" Memory with Accumulator
        void AND(ushort address)
        {
            byte src = memory.ReadByte(address);
            src &= AC;
            SET_SIGN(src);
            SET_ZERO(src);
            AC = src;
        }
        //ASL   Shift Left One Bit (Memory or Accumulator)
        void ASL(ushort address)
        {
            //TODO:Repair ASL
            byte src = memory.ReadByte(address);
            SET_CARRY(src & 0x80);
            src <<= 1;
            src &= 0xff;
            SET_SIGN(src);
            SET_ZERO(src);
            //TODO: STORE src in memory or accumulator depending on addressing mode.
        }
        //BIT   Test Bits in Memory with Accumulator
        void BIT(ushort address)
        {
            byte src = memory.ReadByte(address);
            SET_SIGN(src);
            flagOverflow = (0x40 & src) != 0;   /* Copy bit 6 to OVERFLOW flag. */
            SET_ZERO(src & AC);
        }
        //EOR   "Exclusive-Or" Memory with Accumulator
        void EOR(ushort address)
        {
            byte src = memory.ReadByte(address);
            src ^= AC;
            SET_SIGN(src);
            SET_ZERO(src);
            AC = src;
        }
        //LSR   Shift Right One Bit (Memory or Accumulator)
        void LSR(ushort address)
        {
            //TODO: repair LSR
            byte src = memory.ReadByte(address);
            SET_CARRY(src & 0x01);
            src >>= 1;
            SET_SIGN(src);
            SET_ZERO(src);
            //TODO: STORE src in memory or accumulator depending on addressing mode.
        }
        //ORA   "OR" Memory with Accumulator
        void ORA(ushort address)
        {
            byte src = memory.ReadByte(address);
            src |= AC;
            SET_SIGN(src);
            SET_ZERO(src);
            AC = src;
        }
        //ROL   Rotate One Bit Left (Memory or Accumulator)
        void ROL(ushort address)
        {
            byte src = memory.ReadByte(address);
            src <<= 1;
            if (IF_CARRY()) src |= 0x1;
            SET_CARRY(src > 0xff);
            src &= 0xff;
            SET_SIGN(src);
            SET_ZERO(src);
            //TODO: STORE src in memory or accumulator depending on addressing mode.
        }
        //ROR   Rotate One Bit Right (Memory or Accumulator)
        void ROR(ushort address)
        {
            //TODO: repair ROR
            byte src = memory.ReadByte(address);
            if (IF_CARRY()) src |= 0x100;
            SET_CARRY(src & 0x01);
            src >>= 1;
            SET_SIGN(src);
            SET_ZERO(src);
            //TODO: STORE src in memory or accumulator depending on addressing mode.
        }
        #endregion

        #region Branch

        //BCC   Branch on Carry Clear
        void BCC(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (!IF_CARRY())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }

        //BCS   Branch on Carry Set
        void BCS(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (IF_CARRY())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }
        //BEQ   Branch on Result Zero
        void BEQ(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (IF_ZERO())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }
        //BMI   Branch on Result Minus
        void BMI(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (IF_SIGN())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }
        //BNE   Branch on Result not Zero
        void BNE(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (!IF_ZERO())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }
        //BPL   Branch on Result Plus
        void BPL(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (!IF_SIGN())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }
        //BVC   Branch on Overflow Clear
        void BVC(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (!IF_OVERFLOW())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }
        //BVS   Branch on Overflow Set
        void BVS(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (IF_OVERFLOW())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }
        #endregion

        #region Jump

        //JMP   Jump to New Location
        void JMP(ushort address)
        {
            byte src = memory.ReadByte(address);
            PC = src;
        }
        //JSR   Jump to New Location Saving Return Address
        void JSR(ushort address)
        {
            byte src = memory.ReadByte(address);
            PC--;
            PUSH((byte)((PC >> 8) & 0xff)); /* Push return address onto the stack. */
            PUSH((byte)(PC & 0xff));
            PC = (src);
        }
        //RTI   Return from Interrupt
        void RTI(ushort address)
        {
            //TODO: RTI (set_sr)
            byte src = memory.ReadByte(address);
            src = PULL();
            SET_SR(src);
            src = PULL();
            src |= (byte)((PULL() << 8));   /* Load return address from stack. */
            PC = (src);
        }
        //RTS   Return from Subroutine
        void RTS(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = PULL();
            src += (byte)(((PULL()) << 8) + 1); /* Load return address from stack and add 1. */
            PC = (src);
        }
        #endregion

        #region Registers

        //CLC   Clear Carry Flag
        void CLC()
        {
            SET_CARRY(false);
        }
        //CLD   Clear Decimal Mode
        void CLD()
        {
            SET_DECIMAL(false);
        }
        //CLI   Clear interrupt Disable Bit
        void CLI()
        {
            SET_INTERRUPT(false);
        }
        //CLV   Clear Overflow Flag
        void CLV()
        {
            SET_OVERFLOW(0);
        }
        //CMP   Compare Memory and Accumulator
        void CMP(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = (byte)(AC - src);
            SET_CARRY(src < 0x100);
            SET_SIGN(src);
            SET_ZERO(src &= 0xff);
        }
        //CPX   Compare Memory and Index X
        void CPX(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = (byte)(XR - src);
            SET_CARRY(src < 0x100);
            SET_SIGN(src);
            SET_ZERO(src &= 0xff);
        }
        //CPY   Compare Memory and Index Y
        void CPY(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = (byte)(YR - src);
            SET_CARRY(src < 0x100);
            SET_SIGN(src);
            SET_ZERO(src &= 0xff);
        }
        //SEC   Set Carry Flag
        void SEC()
        {
            SET_CARRY(true);
        }
        //SED   Set Decimal Mode
        void SED()
        {
            SET_DECIMAL(true);
        }
        //SEI   Set Interrupt Disable Status
        void SEI()
        {
            SET_INTERRUPT(true);
        }
        #endregion

        #region Stack

        //PHA   Push Accumulator on Stack
        void PHA()
        {
            byte src;
            src = AC;
            PUSH(src);
        }

        //PHP   Push Processor Status on Stack
        void PHP()
        {
            //TODO: get_sr
            byte src;
            src = GET_SR;
            PUSH(src);
        }

        //PLA   Pull Accumulator from Stack
        void PLA()
        {
            byte src;
            src = PULL();
            SET_SIGN(src);  /* Change sign and zero flag accordingly. */
            SET_ZERO(src);
        }

        //PLP   Pull Processor Status from Stack
        void PLP()
        {
            //TODO: set_sr
            byte src;
            src = PULL();
            SET_SR((src));
        }
        #endregion

        #region System

        //BRK   Force Break
        void BRK()
        {
            PC++;
            PUSH((byte)((PC >> 8) & 0xff)); /* Push return address onto the stack. */
            PUSH((byte)(PC & 0xff));
            SET_BREAK((true));             /* Set BFlag before pushing */
            PUSH(SR);
            SET_INTERRUPT((true));
            PC = (ushort)(memory.ReadByte(0xFFFE) | (memory.ReadByte(0xFFFF) << 8));
        }
        //NOP   No Operation
        void NOP() { }

        #endregion


    }


}
