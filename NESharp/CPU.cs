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

            switch (opCode)
            {
                case 0x00:
                    BRK();
                    break;
                case 0x01:
                    ORA(IndexedIndirect());
                    break;
                case 0x05:
                    ORA(ZeroPage());
                    break;
                case 0x06:
                    ASL(ZeroPage());
                    break;
                case 0x08:
                    PHP();
                    break;
                case 0x09:
                    ORA(Immediate());
                    break;
                case 0x0a:
                    ASL(AC);
                    break;
                case 0x0d:
                    ORA(Absolute());
                    break;
                case 0x0e:
                    ASL(Absolute());
                    break;
                case 0x10:
                    BPL(Relative());
                    break;
                case 0x11:
                    ORA(IndirectIndexed());
                    break;
                case 0x15:
                    ORA(ZeroPageX());
                    break;
                case 0x16:
                    ASL(ZeroPageX());
                    break;
                case 0x18:
                    CLC();
                    break;
                case 0x19:
                    ORA(AbsoluteY());
                    break;
                case 0x1d:
                    ORA(AbsoluteX());
                    break;
                case 0x1e:
                    ASL(AbsoluteX());
                    break;
                case 0x20:
                    JSR(Absolute());
                    break;
                case 0x21:
                    AND(IndexedIndirect());
                    break;
                case 0x24:
                    BIT(ZeroPage());
                    break;
                case 0x25:
                    AND(ZeroPage());
                    break;
                case 0x26:
                    ROL(ZeroPage());
                    break;
                case 0x28:
                    PLP();
                    break;
                case 0x29:
                    AND(Immediate());
                    break;
                case 0x2a:
                    ROL(AC);
                    break;
                case 0x2c:
                    BIT(Absolute());
                    break;
                case 0x2d:
                    AND(Absolute());
                    break;
                case 0x2e:
                    ROL(Absolute());
                    break;
                case 0x30:
                    BMI(Relative());
                    break;
                case 0x31:
                    AND(IndirectIndexed());
                    break;
                case 0x35:
                    AND(ZeroPageX());
                    break;
                case 0x36:
                    ROL(ZeroPageX());
                    break;
                case 0x38:
                    SEC();
                    break;
                case 0x39:
                    AND(AbsoluteY());
                    break;
                case 0x3d:
                    AND(AbsoluteX());
                    break;
                case 0x3e:
                    ROL(AbsoluteX());
                    break;
                case 0x40:
                    RTI();
                    break;
                case 0x41:
                    EOR(IndexedIndirect());
                    break;
                case 0x45:
                    EOR(ZeroPage());
                    break;
                case 0x46:
                    LSR(ZeroPage());
                    break;
                case 0x48:
                    PHA();
                    break;
                case 0x49:
                    EOR(Immediate());
                    break;
                case 0x4a:
                    LSR(AC);
                    break;
                case 0x4c:
                    JMP(AC);
                    break;
                case 0x4d:
                    EOR(Absolute());
                    break;
                case 0x4e:
                    LSR(Absolute());
                    break;
                case 0x50:
                    BVC(Relative());
                    break;
                case 0x51:
                    EOR(IndirectIndexed());
                    break;
                case 0x55:
                    EOR(ZeroPageX());
                    break;
                case 0x56:
                    LSR(ZeroPageX());
                    break;
                case 0x58:
                    CLI();
                    break;
                case 0x59:
                    EOR(AbsoluteY());
                    break;
                case 0x5d:
                    EOR(AbsoluteX());
                    break;
                case 0x5e:
                    LSR(AbsoluteX());
                    break;
                case 0x60:
                    RTS();
                    break;
                case 0x61:
                    ADC(IndexedIndirect());
                    break;
                case 0x65:
                    ADC(ZeroPage());
                    break;
                case 0x66:
                    ROR(ZeroPage());
                    break;
                case 0x68:
                    PLA();
                    break;
                case 0x69:
                    ADC(Immediate());
                    break;
                case 0x6a:
                    ROR(AC);
                    break;
                case 0x6c:
                    JMP(Indirect());
                    break;
                case 0x6d:
                    ADC(Absolute());
                    break;
                case 0x6e:
                    ROR(AbsoluteX());
                    break;
                case 0x70:
                    BVS(Relative());
                    break;
                case 0x71:
                    ADC(IndirectIndexed());
                    break;
                case 0x75:
                    ADC(ZeroPageX());
                    break;
                case 0x76:
                    ROR(ZeroPageX());
                    break;
                case 0x78:
                    SEI();
                    break;
                case 0x79:
                    ADC(AbsoluteY());
                    break;
                case 0x7d:
                    ADC(AbsoluteX());
                    break;
                case 0x7e:
                    ROR(Absolute());
                    break;
                case 0x81:
                    STA(IndexedIndirect());
                    break;
                case 0x84:
                    STY(ZeroPage());
                    break;
                case 0x85:
                    STA(ZeroPage());
                    break;
                case 0x86:
                    STX(ZeroPage());
                    break;
                case 0x88:
                    DEY();
                    break;
                case 0x8a:
                    TXA();
                    break;
                case 0x8c:
                    STY(Absolute());
                    break;
                case 0x8d:
                    STA(Absolute());
                    break;
                case 0x8e:
                    STX(Absolute());
                    break;
                case 0x90:
                    BCC(Relative());
                    break;
                case 0x91:
                    STA(IndirectIndexed());
                    break;
                case 0x94:
                    STY(ZeroPageX());
                    break;
                case 0x95:
                    STA(ZeroPageX());
                    break;
                case 0x96:
                    STX(ZeroPageY());
                    break;
                case 0x98:
                    TYA();
                    break;
                case 0x99:
                    STA(AbsoluteY());
                    break;
                case 0x9a:
                    TXS();
                    break;
                case 0x9d:
                    STA(AbsoluteX());
                    break;
                case 0xa0:
                    LDY(Immediate());
                    break;
                case 0xa1:
                    LDA(IndexedIndirect());
                    break;
                case 0xa2:
                    LDX(Immediate());
                    break;
                case 0xa4:
                    LDY(ZeroPage());
                    break;
                case 0xa5:
                    LDA(ZeroPage());
                    break;
                case 0xa6:
                    LDX(ZeroPage());
                    break;
                case 0xa8:
                    TAY();
                    break;
                case 0xa9:
                    LDA(Immediate());
                    break;
                case 0xaa:
                    TAX();
                    break;
                case 0xac:
                    LDY(Absolute());
                    break;
                case 0xad:
                    LDA(Absolute());
                    break;
                case 0xae:
                    LDX(Absolute());
                    break;
                case 0xb0:
                    BCS(Relative());
                    break;
                case 0xb1:
                    LDA(IndirectIndexed());
                    break;
                case 0xb4:
                    LDY(ZeroPageX());
                    break;
                case 0xb5:
                    LDA(ZeroPageX());
                    break;
                case 0xb6:
                    LDX(ZeroPageY());
                    break;
                case 0xb8:
                    CLV();
                    break;
                case 0xb9:
                    LDA(AbsoluteY());
                    break;
                case 0xba:
                    TSX();
                    break;
                case 0xbc:
                    LDY(AbsoluteX());
                    break;
                case 0xbd:
                    LDA(AbsoluteX());
                    break;
                case 0xbe:
                    LDX(AbsoluteY());
                    break;
                case 0xc0:
                    CPY(Immediate());
                    break;
                case 0xc1:
                    CMP(IndexedIndirect());
                    break;
                case oxc4:
                    CPY(ZeroPage());
                    break;
                case 0xc5:
                    CMP(ZeroPage());
                    break;
                case 0xc6:
                    DEC(ZeroPage());
                    break;
                case 0xc8:
                    INY();
                    break;
                case 0xc9:
                    CMP(Immediate());
                    break;
                case 0xca:
                    DEX();
                    break;
                case 0xcc:
                    CPY(Absolute());
                    break;
                case 0xcd:
                    CMP(Absolute());
                    break;
                case 0xce:
                    DEC(Absolute());
                    break;
                case 0xd0:
                    BNE(Relative());
                    break;
                case 0xd1:
                    CMP(IndirectIndexed());
                    break;
                case 0xd5:
                    CMP(ZeroPageX());
                    break;
                case 0xd6:
                    DEC(ZeroPageX());
                    break;
                case 0xd8:
                    CLD();
                    break;
                case 0xd9:
                    CMP(AbsoluteY());
                    break;
                case 0xdd:
                    CMP(AbsoluteX());
                    break;
                case 0xde:
                    DEC(AbsoluteX());
                    break;
                case 0xe0:
                    CPX(Immediate());
                    break;
                case 0xe1:
                    SBC(IndexedIndirect());
                    break;
                case 0xe4:
                    CPX(ZeroPage());
                    break;
                case 0xe5:
                    SBC(ZeroPage());
                    break;
                case 0xe6:
                    INC(ZeroPage());
                    break;
                case 0xe8:
                    INX();
                    break;
                case 0xe9:
                    SBC(Immediate());
                    break;
                case 0xea:
                    NOP();
                    break;
                case 0xec:
                    CPX(Absolute());
                    break;
                case 0xed:
                    SBC(Absolute());
                    break;
                case 0xee:
                    INC(Absolute());
                    break;
                case 0xf0:
                    BEQ(Relative());
                    break;
                case 0xf1:
                    SBC(IndirectIndexed());
                    break;
                case 0xf5:
                    SBC(ZeroPageX());
                    break;
                case 0xf6:
                    INC(ZeroPage());
                    break;
                case 0xf8:
                    SED();
                    break;
                case 0xf9:
                    SBC(AbsoluteY());
                    break;
                case 0xfd:
                    SBC(AbsoluteX());
                    break;
                case 0xfe:
                    INC(AbsoluteX());
                    break;

                


            }
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
        private void DEX()
        {
            byte src = XR;
            src = (byte)((src - 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            XR = (src);
        }

        //DEY   Decrement Index Y by One
        private void DEY()
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
        private void INX()
        {
            byte src = XR;
            src = (byte)((src + 1) & 0xff);
            SET_SIGN(src);
            SET_ZERO(src);
            XR = (src);
        }

        //INY   Increment Index Y by One
        private void INY()
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
        private void RTI()
        {

            byte src;
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