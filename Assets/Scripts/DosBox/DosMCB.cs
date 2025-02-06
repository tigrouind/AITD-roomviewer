using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DosMCB
{
	public int Position;
	public int Tag;
	public int Owner;
	public int Size;
	public string Name;

	public static DosMCB ReadMCB(byte[] memory, int offset)
	{
		return new DosMCB
		{
			Position = offset + 16,
			Tag = memory[offset],
			Owner = memory.ReadUnsignedShort(offset + 1) * 16,
			Size = memory.ReadUnsignedShort(offset + 3) * 16,
			Name = Encoding.ASCII.GetString(memory, offset + 8, 8).TrimEnd((char)0)
		};
	}

	public static IEnumerable<DosMCB> GetMCBs(byte[] memory)
	{
		int firstMCB = memory.ReadUnsignedShort(0x0826 - 2) * 16; //sysvars (list of lists) + firstMCB offset (-2) (see DOSBox/dos_inc.h)

		//scan DOS memory control block (MCB) chain
		int pos = firstMCB;
		while (pos <= (memory.Length - 16))
		{
			DosMCB block = ReadMCB(memory, pos);
			if (block.Tag != 0x4D && block.Tag != 0x5A)
			{
				break;
			}

			yield return block;

			if (block.Tag == 0x5A) //last tag should be 0x5A
			{
				break;
			}

			pos += block.Size + 16;
		}
	}
}