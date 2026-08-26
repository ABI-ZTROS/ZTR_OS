using System.Runtime.InteropServices;

namespace ZTR.HAL;

/// <summary>
/// Abstraction for the ATKACPI device handle and IO control operations.
/// Enables unit testing without requiring the actual ATKACPI driver.
/// </summary>
public interface IAtkDevice : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the device handle is available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Sends an IO control command to the device with just a success/failure result.
    /// </summary>
    /// <param name="inBuffer">The input buffer.</param>
    /// <param name="outBufferSize">The size of the output buffer.</param>
    /// <returns>True if the call succeeded; otherwise false.</returns>
    bool CallControl(byte[] inBuffer, int outBufferSize);

    /// <summary>
    /// Sends an IO control command to the device and returns the output buffer.
    /// </summary>
    /// <param name="inBuffer">The input buffer.</param>
    /// <param name="outBufferSize">The size of the output buffer.</param>
    /// <returns>The output buffer bytes, or an empty array on failure.</returns>
    byte[] CallControlBuffer(byte[] inBuffer, int outBufferSize);
}

/// <summary>
/// Default implementation of <see cref="IAtkDevice"/> using DeviceIoControl via P/Invoke.
/// </summary>
public class AtkDevice : IAtkDevice
{
    private const uint ControlCode = 0x0022240C;
    private readonly IntPtr _handle;

    /// <inheritdoc />
    public bool IsAvailable { get; }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(
        string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition,
        uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice, uint dwIoControlCode,
        IntPtr lpInBuffer, uint nInBufferSize,
        IntPtr lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    /// <summary>
    /// Creates a new instance of the <see cref="AtkDevice"/> class.
    /// </summary>
    public AtkDevice()
    {
        try
        {
            _handle = CreateFile(@"\\.\ATKACPI", 0xC0000000, 0, IntPtr.Zero, 3, 0x80, IntPtr.Zero);
            IsAvailable = _handle != IntPtr.Zero;
        }
        catch (DllNotFoundException)
        {
            _handle = IntPtr.Zero;
            IsAvailable = false;
        }
        catch (EntryPointNotFoundException)
        {
            _handle = IntPtr.Zero;
            IsAvailable = false;
        }
    }

    /// <summary>
    /// Creates a new instance with a pre-existing handle.
    /// </summary>
    /// <param name="handle">The device handle to use.</param>
    /// <param name="skipInit">If true, skips handle creation.</param>
    internal AtkDevice(IntPtr handle, bool skipInit)
    {
        _handle = handle;
        IsAvailable = handle != IntPtr.Zero;
    }

    /// <inheritdoc />
    public bool CallControl(byte[] inBuffer, int outBufferSize)
    {
        if (!IsAvailable) return false;

        uint bytesReturned;
        IntPtr outBuffer = Marshal.AllocHGlobal(outBufferSize);
        try
        {
            IntPtr inBufferPtr = Marshal.AllocHGlobal(inBuffer.Length);
            Marshal.Copy(inBuffer, 0, inBufferPtr, inBuffer.Length);

            bool result = DeviceIoControl(_handle, ControlCode,
                inBufferPtr, (uint)inBuffer.Length,
                outBuffer, (uint)outBufferSize, out bytesReturned, IntPtr.Zero);

            Marshal.FreeHGlobal(inBufferPtr);
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(outBuffer);
        }
    }

    /// <inheritdoc />
    public byte[] CallControlBuffer(byte[] inBuffer, int outBufferSize)
    {
        if (!IsAvailable) return Array.Empty<byte>();

        uint bytesReturned;
        IntPtr outBuffer = Marshal.AllocHGlobal(outBufferSize);
        try
        {
            IntPtr inBufferPtr = Marshal.AllocHGlobal(inBuffer.Length);
            Marshal.Copy(inBuffer, 0, inBufferPtr, inBuffer.Length);

            bool result = DeviceIoControl(_handle, ControlCode,
                inBufferPtr, (uint)inBuffer.Length,
                outBuffer, (uint)outBufferSize, out bytesReturned, IntPtr.Zero);

            Marshal.FreeHGlobal(inBufferPtr);

            if (result && bytesReturned > 0)
            {
                byte[] buffer = new byte[bytesReturned];
                Marshal.Copy(outBuffer, buffer, 0, (int)bytesReturned);
                return buffer;
            }

            return Array.Empty<byte>();
        }
        finally
        {
            Marshal.FreeHGlobal(outBuffer);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            CloseHandle(_handle);
        }
    }
}