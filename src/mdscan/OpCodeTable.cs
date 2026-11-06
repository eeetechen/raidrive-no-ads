using System.Collections.Generic;

static class OpCodeTable
{
    public class Info { public string Name; public string OperandType; }
    static readonly Dictionary<int, Info> _m = new();

    static void A(int code, string name, string operand) => _m[code] = new Info { Name = name, OperandType = operand };

    static OpCodeTable()
    {
        // single-byte
        A(0x00,"nop","None"); A(0x01,"break","None"); A(0x02,"ldarg.0","None"); A(0x03,"ldarg.1","None");
        A(0x04,"ldarg.2","None"); A(0x05,"ldarg.3","None"); A(0x06,"ldloc.0","None"); A(0x07,"ldloc.1","None");
        A(0x08,"ldloc.2","None"); A(0x09,"ldloc.3","None"); A(0x0A,"stloc.0","None"); A(0x0B,"stloc.1","None");
        A(0x0C,"stloc.2","None"); A(0x0D,"stloc.3","None"); A(0x0E,"ldarg.s","ShortInlineVar"); A(0x0F,"ldarga.s","ShortInlineVar");
        A(0x10,"starg.s","ShortInlineVar"); A(0x11,"ldloc.s","ShortInlineVar"); A(0x12,"ldloca.s","ShortInlineVar");
        A(0x13,"stloc.s","ShortInlineVar"); A(0x14,"ldnull","None"); A(0x15,"ldc.i4.m1","None"); A(0x16,"ldc.i4.0","None");
        A(0x17,"ldc.i4.1","None"); A(0x18,"ldc.i4.2","None"); A(0x19,"ldc.i4.3","None"); A(0x1A,"ldc.i4.4","None");
        A(0x1B,"ldc.i4.5","None"); A(0x1C,"ldc.i4.6","None"); A(0x1D,"ldc.i4.7","None"); A(0x1E,"ldc.i4.8","None");
        A(0x1F,"ldc.i4.s","ShortInlineI"); A(0x20,"ldc.i4","InlineI"); A(0x21,"ldc.i8","InlineI8"); A(0x22,"ldc.r4","ShortInlineR");
        A(0x23,"ldc.r8","InlineR"); A(0x25,"dup","None"); A(0x26,"pop","None"); A(0x27,"jmp","InlineMethod");
        A(0x28,"call","InlineMethod"); A(0x29,"calli","InlineSig"); A(0x2A,"ret","None");
        A(0x2B,"br.s","ShortInlineBrTarget"); A(0x2C,"brfalse.s","ShortInlineBrTarget"); A(0x2D,"brtrue.s","ShortInlineBrTarget");
        A(0x2E,"beq.s","ShortInlineBrTarget"); A(0x2F,"bge.s","ShortInlineBrTarget"); A(0x30,"bgt.s","ShortInlineBrTarget");
        A(0x31,"ble.s","ShortInlineBrTarget"); A(0x32,"blt.s","ShortInlineBrTarget"); A(0x33,"bne.un.s","ShortInlineBrTarget");
        A(0x34,"bge.un.s","ShortInlineBrTarget"); A(0x35,"bgt.un.s","ShortInlineBrTarget"); A(0x36,"ble.un.s","ShortInlineBrTarget");
        A(0x37,"blt.un.s","ShortInlineBrTarget"); A(0x38,"br","InlineBrTarget"); A(0x39,"brfalse","InlineBrTarget");
        A(0x3A,"brtrue","InlineBrTarget"); A(0x3B,"beq","InlineBrTarget"); A(0x3C,"bge","InlineBrTarget");
        A(0x3D,"bgt","InlineBrTarget"); A(0x3E,"ble","InlineBrTarget"); A(0x3F,"blt","InlineBrTarget");
        A(0x40,"bne.un","InlineBrTarget"); A(0x41,"bge.un","InlineBrTarget"); A(0x42,"bgt.un","InlineBrTarget");
        A(0x43,"ble.un","InlineBrTarget"); A(0x44,"blt.un","InlineBrTarget"); A(0x45,"switch","InlineSwitch");
        A(0x46,"ldind.i1","None"); A(0x47,"ldind.u1","None"); A(0x48,"ldind.i2","None"); A(0x49,"ldind.u2","None");
        A(0x4A,"ldind.i4","None"); A(0x4B,"ldind.u4","None"); A(0x4C,"ldind.i8","None"); A(0x4D,"ldind.i","None");
        A(0x4E,"ldind.r4","None"); A(0x4F,"ldind.r8","None"); A(0x50,"ldind.ref","None"); A(0x51,"stind.ref","None");
        A(0x52,"stind.i1","None"); A(0x53,"stind.i2","None"); A(0x54,"stind.i4","None"); A(0x55,"stind.i8","None");
        A(0x56,"stind.r4","None"); A(0x57,"stind.r8","None"); A(0x58,"add","None"); A(0x59,"sub","None");
        A(0x5A,"mul","None"); A(0x5B,"div","None"); A(0x5C,"div.un","None"); A(0x5D,"rem","None"); A(0x5E,"rem.un","None");
        A(0x5F,"and","None"); A(0x60,"or","None"); A(0x61,"xor","None"); A(0x62,"shl","None"); A(0x63,"shr","None");
        A(0x64,"shr.un","None"); A(0x65,"neg","None"); A(0x66,"not","None"); A(0x67,"conv.i1","None"); A(0x68,"conv.i2","None");
        A(0x69,"conv.i4","None"); A(0x6A,"conv.i8","None"); A(0x6B,"conv.r4","None"); A(0x6C,"conv.r8","None");
        A(0x6D,"conv.u4","None"); A(0x6E,"conv.u8","None"); A(0x6F,"callvirt","InlineMethod"); A(0x70,"cpobj","InlineType");
        A(0x71,"ldobj","InlineType"); A(0x72,"ldstr","InlineString"); A(0x73,"newobj","InlineMethod"); A(0x74,"castclass","InlineType");
        A(0x75,"isinst","InlineType"); A(0x76,"conv.r.un","None"); A(0x79,"unbox","InlineType"); A(0x7A,"throw","None");
        A(0x7B,"ldfld","InlineField"); A(0x7C,"ldflda","InlineField"); A(0x7D,"stfld","InlineField"); A(0x7E,"ldsfld","InlineField");
        A(0x7F,"ldsflda","InlineField"); A(0x80,"stsfld","InlineField"); A(0x81,"stobj","InlineType"); A(0x82,"conv.ovf.i1.un","None");
        A(0x8C,"box","InlineType"); A(0x8D,"newarr","InlineType"); A(0x8E,"ldlen","None"); A(0x8F,"ldelema","InlineType");
        A(0x90,"ldelem.i1","None"); A(0x91,"ldelem.u1","None"); A(0x92,"ldelem.i2","None"); A(0x93,"ldelem.u2","None");
        A(0x94,"ldelem.i4","None"); A(0x95,"ldelem.u4","None"); A(0x96,"ldelem.i8","None"); A(0x97,"ldelem.i","None");
        A(0x98,"ldelem.r4","None"); A(0x99,"ldelem.r8","None"); A(0x9A,"ldelem.ref","None"); A(0x9B,"stelem.i","None");
        A(0x9C,"stelem.i1","None"); A(0x9D,"stelem.i2","None"); A(0x9E,"stelem.i4","None"); A(0x9F,"stelem.i8","None");
        A(0xA0,"stelem.r4","None"); A(0xA1,"stelem.r8","None"); A(0xA2,"stelem.ref","None"); A(0xA3,"ldelem","InlineType");
        A(0xA4,"stelem","InlineType"); A(0xA5,"unbox.any","InlineType"); A(0xB3,"conv.ovf.i1","None"); A(0xB4,"conv.ovf.u1","None");
        A(0xB5,"conv.ovf.i2","None"); A(0xB6,"conv.ovf.u2","None"); A(0xB7,"conv.ovf.i4","None"); A(0xB8,"conv.ovf.u4","None");
        A(0xB9,"conv.ovf.i8","None"); A(0xBA,"conv.ovf.u8","None"); A(0xC2,"refanyval","InlineType"); A(0xC3,"ckfinite","None");
        A(0xC6,"mkrefany","InlineType"); A(0xD0,"ldtoken","InlineTok"); A(0xD1,"conv.u2","None"); A(0xD2,"conv.u1","None");
        A(0xD3,"conv.i","None"); A(0xD4,"conv.ovf.i","None"); A(0xD5,"conv.ovf.u","None"); A(0xD6,"add.ovf","None");
        A(0xD7,"add.ovf.un","None"); A(0xD8,"mul.ovf","None"); A(0xD9,"mul.ovf.un","None"); A(0xDA,"sub.ovf","None");
        A(0xDB,"sub.ovf.un","None"); A(0xDC,"endfinally","None"); A(0xDD,"leave","InlineBrTarget"); A(0xDE,"leave.s","ShortInlineBrTarget");
        A(0xDF,"stind.i","None"); A(0xE0,"conv.u","None");
        // two-byte (0xFE prefix -> code | 0xFE00)
        A(0xFE00,"arglist","None"); A(0xFE01,"ceq","None"); A(0xFE02,"cgt","None"); A(0xFE03,"cgt.un","None");
        A(0xFE04,"clt","None"); A(0xFE05,"clt.un","None"); A(0xFE06,"ldftn","InlineMethod"); A(0xFE07,"ldvirtftn","InlineMethod");
        A(0xFE09,"ldarg","InlineVar"); A(0xFE0A,"ldarga","InlineVar"); A(0xFE0B,"starg","InlineVar"); A(0xFE0C,"ldloc","InlineVar");
        A(0xFE0D,"ldloca","InlineVar"); A(0xFE0E,"stloc","InlineVar"); A(0xFE0F,"localloc","None"); A(0xFE11,"endfilter","None");
        A(0xFE12,"unaligned.","ShortInlineI"); A(0xFE13,"volatile.","None"); A(0xFE14,"tail.","None"); A(0xFE15,"initobj","InlineType");
        A(0xFE16,"constrained.","InlineType"); A(0xFE17,"cpblk","None"); A(0xFE18,"initblk","None"); A(0xFE19,"no.","ShortInlineI");
        A(0xFE1A,"rethrow","None"); A(0xFE1C,"sizeof","InlineType"); A(0xFE1D,"refanytype","None"); A(0xFE1E,"readonly.","None");
    }

    public static Info From(int code)
    {
        if (_m.TryGetValue(code, out var i)) return i;
        return new Info { Name = $"op_{code:x4}", OperandType = "None" };
    }
}
