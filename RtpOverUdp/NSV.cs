using System;
using System.Runtime.InteropServices;
using System.Security;

namespace NSVWrapper
{
    public class NSV
    {
        const string DLL_NAME = "NSVideo.dll";

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Create();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Release(uint nsv);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SetDecoder(uint nsv,
            [MarshalAs(UnmanagedType.LPStr)]string name, byte pt = 0);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AttachWindow(uint nsv, IntPtr h);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Start(uint nsv);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Stop(uint nsv);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetDecodeParameter(uint nsv,
            [MarshalAs(UnmanagedType.LPStr)]string parm);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PushMediaPacket(uint nsv,
            [MarshalAs(UnmanagedType.LPArray)]byte[] pData, int size);
    }
}
