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
    }
}
