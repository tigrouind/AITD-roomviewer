using System.Runtime.InteropServices;
using System;
using System.Text;

public class ProcessMemoryReader
{
	const int PROCESS_QUERY_INFORMATION = 0x0400;
	const int PROCESS_VM_READ = 0x0010;
	const int PROCESS_VM_WRITE = 0x0020;
	const int PROCESS_VM_OPERATION = 0x0008;
	const int MEM_COMMIT = 0x00001000;
	const int MEM_PRIVATE = 0x20000;
	const int PAGE_READWRITE = 0x04;

	[StructLayout(LayoutKind.Sequential)]
	private struct MEMORY_BASIC_INFORMATION
	{
		public IntPtr BaseAddress;
		public IntPtr AllocationBase;
		public uint AllocationProtect;
		public IntPtr RegionSize;
		public int State;
		public int Protect;
		public int Type;
	}

	[DllImport("kernel32.dll")]
	private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

	[DllImport("kernel32.dll")]
	private static extern bool CloseHandle(IntPtr hObject);

	[DllImport("kernel32.dll")]
	private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

	[DllImport("kernel32.dll")]
	private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

	[DllImport("kernel32.dll")]
	private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

	private IntPtr processHandle;

	public ProcessMemoryReader(int processId)
	{
		this.processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, processId);
	}

	~ProcessMemoryReader()
	{
		Close();
	}

	public long Read(byte[] buffer, long offset, int count)
	{
		IntPtr bytesRead;
		if (ReadProcessMemory(processHandle, new IntPtr(offset), buffer, count, out bytesRead))
		{
			return (long)bytesRead;
		}
		return 0;
	}

	public long Write(byte[] buffer, long offset, int count)
	{
		IntPtr bytesWritten;
		if (WriteProcessMemory(processHandle, new IntPtr(offset), buffer, count, out bytesWritten))
		{
			return (long)bytesWritten;
		}
		return 0;
	}

	public void Close()
	{
		if (processHandle != IntPtr.Zero)
		{
			CloseHandle(processHandle);
			processHandle = IntPtr.Zero;
		}
	}

	public long SearchFor16MRegion()
	{
		MEMORY_BASIC_INFORMATION mem_info = new MEMORY_BASIC_INFORMATION();

		long min_address = 0;
		long max_address = 0x7FFFFFFF;

		//scan process memory regions
		while (min_address < max_address
			&& VirtualQueryEx(processHandle, (IntPtr)min_address, out mem_info, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) > 0)
		{
			//check if memory region is accessible
			//skip regions smaller than 16M (default DOSBOX memory size)
			if (mem_info.Protect == PAGE_READWRITE && mem_info.State == MEM_COMMIT && (mem_info.Type & MEM_PRIVATE) == MEM_PRIVATE
				&& (int)mem_info.RegionSize >= 1024 * 1024 * 16
				&& SearchForBytePattern(Encoding.ASCII.GetBytes("CON "), (long)mem_info.BaseAddress, 4096) != -1)
			{
				return (long)mem_info.BaseAddress + 32; //skip Windows 32-bytes memory allocation header
			}

			// move to next memory region
			min_address = (long)mem_info.BaseAddress + (long)mem_info.RegionSize;
		}

		return -1;
	}

	long SearchForBytePattern(byte[] pattern, long baseAddress, int bytesToRead = 640 * 1024)
	{
		byte[] buffer = new byte[81920];

		long readPosition = baseAddress;
		long bytesRead;
		while (bytesToRead > 0 && (bytesRead = Read(buffer, readPosition, Math.Min(buffer.Length, bytesToRead))) > 0)
		{
			//search bytes pattern
			int index = Utils.IndexOf(buffer, pattern, 0, (int)bytesRead);
			if (index != -1)
			{
				return readPosition + index;
			}

			readPosition += bytesRead;
			bytesToRead -= (int)bytesRead;
		}

		return -1;
	}
}