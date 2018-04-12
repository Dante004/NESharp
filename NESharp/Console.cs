using System.Threading;
using System.Diagnostics;

namespace NESharp
{
    internal class Console
    {
        public CPU cpu;
        private CPUMemory cpuMemory;
        public Mappers mapper;
        public Cartridge cartridge;

        public bool Stop;

        public Console()
        {
            cpuMemory = new CPUMemory(this);
            cpu = new CPU(cpuMemory);
        }

        public void LoadCartridge(string path)
        {
            cartridge = new Cartridge(path);
            switch (cartridge.mapperNumber)
            {
                case 0:
                    mapper = new NROM(this);
                    break;

                default:
                    System.Console.WriteLine("Mapper is not supported");
                    break;
            }
            cpu.Reset();
            cpuMemory.Reset();
        }

        public void Cycle()
        {
            cpu.Step();
        }
    }
}