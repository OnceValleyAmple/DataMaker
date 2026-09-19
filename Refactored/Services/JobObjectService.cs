using System.Diagnostics; using System.Runtime.InteropServices;
namespace DataMaker.Refactored.Services;
public sealed class JobObjectService:IDisposable
{ const int ExtendedInfo=9; const uint ProcessMemory=0x100; nint handle;
 [DllImport("kernel32.dll",SetLastError=true)] static extern nint CreateJobObject(nint a,string? name);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool SetInformationJobObject(nint h,int type,ref ExtendedLimits info,uint size);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool AssignProcessToJobObject(nint h,nint process);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool TerminateJobObject(nint h,uint code);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool CloseHandle(nint h);
 [StructLayout(LayoutKind.Sequential)] struct Basic{public ulong ProcessTime;public ulong JobTime;public uint Flags;public nuint Min;public nuint Max;public uint Active;public nuint Affinity;public uint Priority;public uint Scheduling;}
 [StructLayout(LayoutKind.Sequential)] struct Io{public ulong Read;public ulong Write;public ulong Other;}
 [StructLayout(LayoutKind.Sequential)] struct ExtendedLimits{public Basic Basic;public Io Io;public ulong ProcessMemory;public ulong JobMemory;public ulong PeakProcess;public ulong PeakJob;}
 public static JobObjectService? TryCreate(int memoryMb){if(memoryMb<=0)return null;var j=new JobObjectService{handle=CreateJobObject(0,null)};if(j.handle==0)return null;var info=new ExtendedLimits{Basic=new Basic{Flags=ProcessMemory},ProcessMemory=(ulong)memoryMb*1024*1024};if(!SetInformationJobObject(j.handle,ExtendedInfo,ref info,(uint)Marshal.SizeOf<ExtendedLimits>())){j.Dispose();return null;}return j;}
 public bool TryAssign(Process process)=>handle!=0&&AssignProcessToJobObject(handle,process.Handle);
 public void Kill(){if(handle!=0)TerminateJobObject(handle,1);}
 public void Dispose(){if(handle!=0)CloseHandle(handle);handle=0;}
}
