using System.Collections.Generic;
using TroveSkip.ViewModels;

namespace TroveSkip.Memory
{
    public enum InstructionArgument
    {
        Reg8,
        Const8,
        RegReg8,
        RegMem8,
        MemReg8,
        MemMem8,
        Reg16,
        Const16,
        RegReg16,
        RegMem16,
        MemReg16,
        MemMem16,
    }
    
    // public class ASMInstruction
    // {
    //     public readonly MainWindowViewModel.InstructionsEnum OpCode;
    //     public readonly byte Offset;
    //
    //     // public Dictionary<InstructionArgument, ASMInstruction>
    //     // {
    //     //     
    //     // }
    //
    //     public ASMInstruction(MainWindowViewModel.InstructionsEnum opcode)
    //     {
    //         OpCode = opcode;
    //     }
    //
    //     public ASMInstruction(MainWindowViewModel.InstructionsEnum opcode, byte offset) : this(opcode)
    //     {
    //         Offset = offset;
    //     }
    // }
}