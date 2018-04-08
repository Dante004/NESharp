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
//CPU speed			1.79 MHz	1.66 MHz
namespace NESharp
{
    internal class CPU
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
        private bool flagCarry, flagZero, flagInterrupt, flagDecimal, flagBreak, flagOverflow, flagNegative;

        private byte AC, XR, YR; //AC - accumulator, XR - X register, YR - Y register
        private ushort PC, SP; //PC - program counter, SP - stack pointer
        private int clk; //clk : the number of cycles an instruction takes.
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

        public void Step()
        {
            byte opCode = memory.ReadByte(PC);
            ushort address = 0;
        }

        private void negzero(byte value)
        {
            flagNegative = !(value >= 0x0 && value <= 0x7F);
            flagZero = value == 0x00;
        }

        #region INSTRUCTION OPERATION

        private void SET_ZERO(int x)
        {
            flagZero = x == 0;
        }

        private void SET_SIGN(int x)
        {
            flagNegative = !(x >= 0x0 && x <= 0x7F);
        }

        private void SET_OVERFLOW(int x)
        {
            flagOverflow = x > 255;
        }

        private void SET_CARRY(int x)
        {
            flagCarry = x != 0;
        }

        private void SET_CARRY(bool x)
        {
            flagCarry = x;
        }

        private void SET_DECIMAL(bool x)
        {
            flagDecimal = x;
        }

        private void SET_INTERRUPT(bool x)
        {
            flagInterrupt = x;
        }

        private void SET_BREAK(bool x)
        {
            flagBreak = x;
        }

        private bool IF_CARRY()
        {
            return flagCarry;
        }

        private bool IF_OVERFLOW()
        {
            return flagOverflow;
        }

        private bool IF_SIGN()
        {
            return flagNegative;
        }

        private bool IF_ZERO()
        {
            return flagZero;
        }

        private bool IF_DECIMAL()
        {
            return flagDecimal;
        }

        //REL_ADDR(PC, src) : returns the relative address obtained by adding the displacement src to the PC.
        private ushort REL_ADDR(ushort pc, byte src)
        {
            return (ushort)(pc + src);
        }

        private byte PULL()
        {
            SP++;
            byte data = memory.ReadByte((ushort)(0x100 | SP));
            return data;
        }

        private void PUSH(byte data)
        {
            memory.WriteByte((ushort)(0x100 | SP), data);
            SP--;
        }

        //GET_SR    get the value of the Program Status Register.
        private byte GET_SR()
        {
            byte flags = 0;

            if (flagCarry) flags |= (byte)(1 << 0); // Carry flag, bit 0
            if (flagZero) flags |= (byte)(1 << 1); // Zero flag, bit 1
            if (flagInterrupt) flags |= (byte)(1 << 2); // Interrupt disable flag, bit 2
            if (flagDecimal) flags |= (byte)(1 << 3); // Decimal mode flag, bit 3
            if (flagBreak) flags |= (byte)(1 << 4); // Break mode, bit 4
            flags |= (byte)(1 << 5); // Bit 5, always set
            if (flagOverflow) flags |= (byte)(1 << 6); // Overflow flag, bit 6
            if (flagNegative) flags |= (byte)(1 << 7); // Negative flag, bit 7

            return flags;
        }

        private bool GetBit(byte b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }

        //SET_SR    set the Program Status Register to the value given.
        private void SET_SR(byte flags)
        {
            flagCarry = GetBit(flags, 0);
            flagZero = GetBit(flags, 1);
            flagInterrupt = GetBit(flags, 2);
            flagDecimal = GetBit(flags, 3);
            flagBreak = GetBit(flags, 4);
            flagOverflow = GetBit(flags, 6);
            flagNegative = GetBit(flags, 7);
        }

        #endregion INSTRUCTION OPERATION

        #region storage

        //LDA (Load Accumulator With Memory)
        private void LDA(ushort address)
        {
            AC = memory.ReadByte(address);
            negzero(AC);
        }

        //LDX (Load X Index With Memory)
        private void LDX(ushort address)
        {
            XR = memory.ReadByte(address);
            negzero(XR);
        }

        //LDY (Load Y Index With Memory)
        private void LDY(ushort address)
        {
            YR = memory.ReadByte(address);
            negzero(YR);
        }

        //STA (Store Accumulator In Memory)
        private void STA(ushort address)
        {
            memory.WriteByte(address, AC);
        }

        //STX (Store X Index In Memory)
        private void STX(ushort address)
        {
            memory.WriteByte(address, XR);
        }

        //STY (Store Y Index In Memory)
        private void STY(ushort address)
        {
            memory.WriteByte(address, YR);
        }

        //TAX (Transfer Accumulator to X Index)
        private void TAX()
        {
            negzero(AC);
            XR = AC;
        }

        //TAY (Transfer Accumulator to Y Index)
        private void TAY()
        {
            negzero(AC);
            YR = AC;
        }

        //TSX (Transfer Stack Pointer to X Index)
        private void TSX()
        {
            negzero(XR);
            XR = (byte)SP;
        }

        //TXA (Transfer X Index to Accumulator)
        private void TXA()
        {
            negzero(XR);
            AC = XR;
        }

        //TXS (Transfer X Index to Stack Pointer)
        private void TXS()
        {
            negzero(XR);
            SP = XR;
        }

        //TYA (Transfer Y Index to Accumulator)
        private void TYA()
        {
            negzero(YR);
            AC = YR;
        }

        #endregion storage

        #region Math

        //ADC   Add Memory to Accumulator with Carry
        private void ADC(ushort address)
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
        private void DEC(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = (byte)((src - 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            memory.WriteByte(address, src);
        }

        //DEX   Decrement Index X by One
        private void DEX(ushort address)
        {
            byte src = XR;
            src = (byte)((src - 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            XR = (src);
        }

        //DEY   Decrement Index Y by One
        private void DEY(ushort address)
        {
            byte src = YR;
            src = (byte)((src - 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            YR = (src);
        }

        //INC   Increment Memory by One
        private void INC(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = (byte)((src + 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            memory.WriteByte(address, src);
        }

        //INX   Increment Index X by One
        private void INX(ushort address)
        {
            byte src = XR;
            src = (byte)((src + 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            XR = (src);
        }

        //INY   Increment Index Y by One
        private void INY(ushort address)
        {
            byte src = YR;
            src = (byte)((src + 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            YR = (src);
        }

        //SBC   Subtract Memory from Accumulator with Borrow
        private void SBC(ushort address)
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

        #endregion Math

        #region Bitwise

        //AND   "AND" Memory with Accumulator
        private void AND(ushort address)
        {
            byte src = memory.ReadByte(address);
            src &= AC;
            SET_SIGN(src);
            SET_ZERO(src);
            AC = src;
        }

        //ASL   Shift Left One Bit (Memory or Accumulator)
        private void ASL(ushort address)
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
        private void BIT(ushort address)
        {
            byte src = memory.ReadByte(address);
            SET_SIGN(src);
            flagOverflow = (0x40 & src) != 0;   /* Copy bit 6 to OVERFLOW flag. */
            SET_ZERO(src & AC);
        }

        //EOR   "Exclusive-Or" Memory with Accumulator
        private void EOR(ushort address)
        {
            byte src = memory.ReadByte(address);
            src ^= AC;
            SET_SIGN(src);
            SET_ZERO(src);
            AC = src;
        }

        //LSR   Shift Right One Bit (Memory or Accumulator)
        private void LSR(ushort address)
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
        private void ORA(ushort address)
        {
            byte src = memory.ReadByte(address);
            src |= AC;
            SET_SIGN(src);
            SET_ZERO(src);
            AC = src;
        }

        //ROL   Rotate One Bit Left (Memory or Accumulator)
        private void ROL(ushort address)
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
        private void ROR(ushort address)
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

        #endregion Bitwise

        #region Branch

        //BCC   Branch on Carry Clear
        private void BCC(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (!IF_CARRY())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }

        //BCS   Branch on Carry Set
        private void BCS(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (IF_CARRY())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }

        //BEQ   Branch on Result Zero
        private void BEQ(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (IF_ZERO())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }

        //BMI   Branch on Result Minus
        private void BMI(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (IF_SIGN())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }

        //BNE   Branch on Result not Zero
        private void BNE(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (!IF_ZERO())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }

        //BPL   Branch on Result Plus
        private void BPL(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (!IF_SIGN())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }

        //BVC   Branch on Overflow Clear
        private void BVC(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (!IF_OVERFLOW())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }

        //BVS   Branch on Overflow Set
        private void BVS(ushort address)
        {
            byte src = memory.ReadByte(address);
            if (IF_OVERFLOW())
            {
                clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                PC = REL_ADDR(PC, src);
            }
        }

        #endregion Branch

        #region Jump

        //JMP   Jump to New Location
        private void JMP(ushort address)
        {
            byte src = memory.ReadByte(address);
            PC = src;
        }

        //JSR   Jump to New Location Saving Return Address
        private void JSR(ushort address)
        {
            byte src = memory.ReadByte(address);
            PC--;
            PUSH((byte)((PC >> 8) & 0xff)); /* Push return address onto the stack. */
            PUSH((byte)(PC & 0xff));
            PC = (src);
        }

        //RTI   Return from Interrupt
        private void RTI(ushort address)
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
        private void RTS(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = PULL();
            src += (byte)(((PULL()) << 8) + 1); /* Load return address from stack and add 1. */
            PC = (src);
        }

        #endregion Jump

        #region Registers

        //CLC   Clear Carry Flag
        private void CLC()
        {
            SET_CARRY(false);
        }

        //CLD   Clear Decimal Mode
        private void CLD()
        {
            SET_DECIMAL(false);
        }

        //CLI   Clear interrupt Disable Bit
        private void CLI()
        {
            SET_INTERRUPT(false);
        }

        //CLV   Clear Overflow Flag
        private void CLV()
        {
            SET_OVERFLOW(0);
        }

        //CMP   Compare Memory and Accumulator
        private void CMP(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = (byte)(AC - src);
            SET_CARRY(src < 0x100);
            SET_SIGN(src);
            SET_ZERO(src &= 0xff);
        }

        //CPX   Compare Memory and Index X
        private void CPX(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = (byte)(XR - src);
            SET_CARRY(src < 0x100);
            SET_SIGN(src);
            SET_ZERO(src &= 0xff);
        }

        //CPY   Compare Memory and Index Y
        private void CPY(ushort address)
        {
            byte src = memory.ReadByte(address);
            src = (byte)(YR - src);
            SET_CARRY(src < 0x100);
            SET_SIGN(src);
            SET_ZERO(src &= 0xff);
        }

        //SEC   Set Carry Flag
        private void SEC()
        {
            SET_CARRY(true);
        }

        //SED   Set Decimal Mode
        private void SED()
        {
            SET_DECIMAL(true);
        }

        //SEI   Set Interrupt Disable Status
        private void SEI()
        {
            SET_INTERRUPT(true);
        }

        #endregion Registers

        #region Stack

        //PHA   Push Accumulator on Stack
        private void PHA()
        {
            byte src;
            src = AC;
            PUSH(src);
        }

        //PHP   Push Processor Status on Stack
        private void PHP()
        {
            //TODO: get_sr
            byte src;
            src = GET_SR();
            PUSH(src);
        }

        //PLA   Pull Accumulator from Stack
        private void PLA()
        {
            byte src;
            src = PULL();
            SET_SIGN(src);  /* Change sign and zero flag accordingly. */
            SET_ZERO(src);
        }

        //PLP   Pull Processor Status from Stack
        private void PLP()
        {
            //TODO: set_sr
            byte src;
            src = PULL();
            SET_SR((src));
        }

        #endregion Stack

        #region System

        //BRK   Force Break
        private void BRK()
        {
            PC++;
            PUSH((byte)((PC >> 8) & 0xff)); /* Push return address onto the stack. */
            PUSH((byte)(PC & 0xff));
            SET_BREAK((true));             /* Set BFlag before pushing */
            PUSH(GET_SR());
            SET_INTERRUPT((true));
            PC = (ushort)(memory.ReadByte(0xFFFE) | (memory.ReadByte(0xFFFF) << 8));
        }

        //NOP   No Operation
        private void NOP()
        { }

        #endregion System

        #region AddressingMode

        private ushort Immediate()
        {
            return (ushort)(PC + 1);
        }

        private ushort Absolute()
        {
            return memory.ReadByte16((ushort)(PC + 1));
        }

        private ushort AbsoluteX()
        {
            return memory.ReadByte16((ushort)((PC + 1) + XR));
        }

        private ushort AbsoluteY()
        {
            return memory.ReadByte16((ushort)((PC + 1) + YR));
        }

        private ushort Relative()
        {
            return (ushort)(PC + memory.ReadByte((ushort)(PC + 1)) + 2);
        }

        private ushort ZeroPage()
        {
            return memory.ReadByte((ushort)(PC + 1));
        }

        private ushort ZeroPageY()
        {
            return (ushort)((memory.ReadByte((ushort)(PC + 1)) + YR) & 0xFF);
        }

        private ushort ZeroPageX()
        {
            return (ushort)((memory.ReadByte((ushort)(PC + 1)) + XR) & 0xFF);
        }

        private ushort Indirect()
        {
            return memory.Read16WrapPage(memory.ReadByte16((ushort)(PC + 1)));
        }

        private ushort IndexedIndirect()
        {
            // Zeropage address of lower nibble of target address (& 0xFF to wrap at 255)
            ushort lowerNibbleAddress = (ushort)((memory.ReadByte((ushort)(PC + 1)) + XR) & 0xFF);

            // Target address (Must wrap to 0x00 if at 0xFF)
            return (ushort)memory.Read16WrapPage((ushort)(lowerNibbleAddress));
        }

        private ushort IndirectIndexed()
        {
            // Zeropage address of the value to add the Y register to to get the target address
            ushort valueAddress = (ushort)memory.ReadByte((ushort)(PC + 1));

            // Target address (Must wrap to 0x00 if at 0xFF)
            return (ushort)(memory.Read16WrapPage(valueAddress) + YR);
        }

        #endregion AddressingMode
    }
}