using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace OldMapStarter
{
    public class CopyFileProgress
    {
        public CopyFileProgress()
        {
        }

        [DllImport("kernel32.dll", EntryPoint = "CopyFileEx", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _CopyFileEx(string lpExistingFileName, string lpNewFileName, CopyProgressRoutine lpProgressRoutine, IntPtr lpData, ref Int32 pbCancel, CopyFileFlags dwCopyFlags);

        private delegate CopyProgressResult CopyProgressRoutine(long TotalFileSize, long TotalBytesTransferred, long StreamSize, long StreamBytesTransferred, uint dwStreamNumber, CopyProgressCallbackReason dwCallbackReason, IntPtr hSourceFile, IntPtr hDestinationFile, IntPtr lpData);

        public enum CopyProgressResult : uint
        {
            PROGRESS_CONTINUE = 0,
            PROGRESS_CANCEL = 1,
            PROGRESS_STOP = 2,
            PROGRESS_QUIET = 3
        }
        public enum CopyProgressCallbackReason : uint
        {
            CALLBACK_CHUNK_FINISHED = 0x00000000,
            CALLBACK_STREAM_SWITCH = 0x00000001
        }
        private enum CopyFileFlags : uint
        {
            COPY_FILE_ALLOW_DECRYPTED_DESTINATION = 0x00000008,
            COPY_FILE_COPY_SYMLINK = 0x00000800,
            COPY_FILE_FAIL_IF_EXISTS = 0x00000001,
            COPY_FILE_NO_BUFFERING = 0x000010000,
            COPY_FILE_OPEN_SOURCE_FOR_WRITE = 0x00000004,
            COPY_FILE_RESTARTABLE = 0x00000002
        }
        public class CopyProgressEventArgs : EventArgs
        {
            public long TotalFileSize { get; internal set; }
            public long TotalBytesTransferred { get; internal set; }
            public long StreamSize { get; internal set; }
            public long StreamBytesTransferred { get; internal set; }
            public uint StreamNumber { get; internal set; }
            public CopyProgressCallbackReason CallbackReason { get; internal set; }
            public CopyProgressResult CopyProgressResult { get; set; }
        }
        private CopyProgressEventArgs _CopyProgressEventArgs = new CopyProgressEventArgs();
        public delegate void CopyProgressEventHandler(object s, CopyProgressEventArgs e);
        public event CopyProgressEventHandler ProgressChanged;
        private int _CopyCancel;

        public enum ResultStatus
        {
            Completed,
            Failed,
            Stoped
        }

        private const int WIN32_ERROR_CODE_SUSPENDED = 1235;
        public ResultStatus CopyStart(string sourceFilePath, string destinationFilePath, bool overWrite)
        {
            CopyFileFlags ov = overWrite
             ? CopyFileFlags.COPY_FILE_RESTARTABLE
                : CopyFileFlags.COPY_FILE_FAIL_IF_EXISTS;

            bool isSuccess = _CopyFileEx(sourceFilePath, destinationFilePath, new CopyProgressRoutine(CopyProgressRoutineCallBack), IntPtr.Zero, ref _CopyCancel, ov);
            if (isSuccess)
            {
                return ResultStatus.Completed;
            }

            int errCode = Marshal.GetLastWin32Error();
            if (errCode == WIN32_ERROR_CODE_SUSPENDED)
            {
                return ResultStatus.Stoped;
            }
            return ResultStatus.Failed;
        }
        private CopyProgressResult CopyProgressRoutineCallBack(long totalFileSize, long totalBytesTransferred, long streamSize, long streamBytesTransferred, uint streamNumber, CopyProgressCallbackReason callbackReason, IntPtr hSourceFile, IntPtr hDestinationFile, IntPtr lpData)
        {
            if (ProgressChanged == null)
            {
                return CopyProgressResult.PROGRESS_CONTINUE;
            }
            _CopyProgressEventArgs.TotalFileSize = totalFileSize;
            _CopyProgressEventArgs.TotalBytesTransferred = totalBytesTransferred;
            _CopyProgressEventArgs.StreamSize = streamSize;
            _CopyProgressEventArgs.StreamBytesTransferred = streamBytesTransferred;
            _CopyProgressEventArgs.StreamNumber = streamNumber;
            _CopyProgressEventArgs.CallbackReason = callbackReason;
            _CopyProgressEventArgs.CopyProgressResult = CopyProgressResult.PROGRESS_CONTINUE;

            ProgressChanged(this, _CopyProgressEventArgs);
            return _CopyProgressEventArgs.CopyProgressResult;
        }
    }
}
