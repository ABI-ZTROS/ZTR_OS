using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class AsusHidTests
{
    private Mock<IHidReportWriter> _mockWriter;
    private AsusHid _hid;

    public AsusHidTests()
    {
        _mockWriter = new Mock<IHidReportWriter>();
        _hid = new AsusHid(_mockWriter.Object);
    }

    private void RegisterDevice(int pid)
    {
        _hid.RegisterStream(pid);
    }

    #region PID Matching Tests

    [Fact]
    public void MainAuraPids_ContainsExpectedIds()
    {
        var pids = AsusHid.MainAuraPids;

        Assert.Contains(0x1a30, pids);
        Assert.Contains(0x1854, pids);
        Assert.Contains(0x1869, pids);
        Assert.Contains(0x1866, pids);
        Assert.Contains(0x19b6, pids);
        Assert.Contains(0x1cd7, pids);
        Assert.Contains(0x1cd8, pids);
        Assert.Contains(0x8854, pids);
    }

    [Fact]
    public void XgmPids_ContainsExpectedIds()
    {
        var pids = AsusHid.XgmPids;

        Assert.Contains(0x1970, pids);
        Assert.Contains(0x1a9a, pids);
        Assert.Contains(0x1C28, pids);
        Assert.Contains(0x1C29, pids);
        Assert.Contains(0x1BC1, pids);
    }

    [Fact]
    public void RearLightPids_ContainsExpectedIds()
    {
        var pids = AsusHid.RearLightPids;

        Assert.Contains(0x18c6, pids);
        Assert.Single(pids);
    }

    [Fact]
    public void IsMainAuraPid_ReturnsTrueForValidPids()
    {
        Assert.True(AsusHid.IsMainAuraPid(0x1a30));
        Assert.True(AsusHid.IsMainAuraPid(0x1854));
        Assert.True(AsusHid.IsMainAuraPid(0x1cd7));
        Assert.True(AsusHid.IsMainAuraPid(0x1cd8));
    }

    [Fact]
    public void IsMainAuraPid_ReturnsFalseForInvalidPids()
    {
        Assert.False(AsusHid.IsMainAuraPid(0xFFFF));
        Assert.False(AsusHid.IsMainAuraPid(0x0000));
        Assert.False(AsusHid.IsMainAuraPid(0x18c6));
        Assert.False(AsusHid.IsMainAuraPid(0x1970));
    }

    [Fact]
    public void IsXgmPid_ReturnsTrueForValidPids()
    {
        Assert.True(AsusHid.IsXgmPid(0x1970));
        Assert.True(AsusHid.IsXgmPid(0x1a9a));
    }

    [Fact]
    public void IsXgmPid_ReturnsFalseForInvalidPids()
    {
        Assert.False(AsusHid.IsXgmPid(0x1a30));
        Assert.False(AsusHid.IsXgmPid(0xFFFF));
    }

    [Fact]
    public void IsRearLightPid_ReturnsTrueForValidPid()
    {
        Assert.True(AsusHid.IsRearLightPid(0x18c6));
    }

    [Fact]
    public void IsRearLightPid_ReturnsFalseForInvalidPids()
    {
        Assert.False(AsusHid.IsRearLightPid(0x1a30));
        Assert.False(AsusHid.IsRearLightPid(0xFFFF));
    }

    #endregion

    #region Report ID Constants Tests

    [Fact]
    public void ReportIds_HaveCorrectValues()
    {
        Assert.Equal(0x5A, AsusHid.INPUT_ID);
        Assert.Equal(0x5D, AsusHid.AURA_ID);
        Assert.Equal(0x5E, AsusHid.XGM_REPORT_ID);
    }

    [Fact]
    public void AsusVid_HaveCorrectValue()
    {
        Assert.Equal(0x0B05, AsusHid.ASUS_VID);
    }

    #endregion

    #region WriteInput Tests

    [Fact]
    public void WriteInput_PrependsInputId()
    {
        RegisterDevice(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        byte[] data = { 0x01, 0x02, 0x03 };
        _hid.WriteInput(data, "test");

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d.Length == 4 && d[0] == AsusHid.INPUT_ID && d[1] == 0x01 && d[2] == 0x02 && d[3] == 0x03)
        ), Times.AtLeastOnce);
    }

    [Fact]
    public void WriteInput_EmptyData_PrependsInputId()
    {
        RegisterDevice(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        byte[] data = { };
        _hid.WriteInput(data);

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d.Length == 1 && d[0] == AsusHid.INPUT_ID)
        ), Times.AtLeastOnce);
    }

    #endregion

    #region Write (Aura) Tests

    [Fact]
    public void WriteAura_PrependsAuraId()
    {
        RegisterDevice(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        byte[] data = { 0xB3, 0x00, 0xFF };
        _hid.Write(data, "test");

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d.Length == 4 && d[0] == AsusHid.AURA_ID && d[1] == 0xB3 && d[2] == 0x00 && d[3] == 0xFF)
        ), Times.AtLeastOnce);
    }

    #endregion

    #region WriteXgm Tests

    [Fact]
    public void WriteXgm_PrependsXgmId()
    {
        RegisterDevice(0x1970);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        byte[] data = { 0x01, 0x02 };
        _hid.WriteXgm(data, "test");

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d.Length == 3 && d[0] == AsusHid.XGM_REPORT_ID && d[1] == 0x01 && d[2] == 0x02)
        ), Times.AtLeastOnce);
    }

    #endregion

    #region SetFeatureAura Tests

    [Fact]
    public void SetFeatureAura_PrependsAuraId()
    {
        RegisterDevice(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        byte[] data = { 0x01, 0x02, 0x03 };
        _hid.SetFeatureAura(data, false);

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d.Length == 4 && d[0] == AsusHid.AURA_ID)
        ), Times.AtLeastOnce);
    }

    [Fact]
    public void SetFeatureAura_WithRetry_RetriesOnFailure()
    {
        RegisterDevice(0x1a30);
        int callCount = 0;
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Callback(() =>
            {
                callCount++;
                if (callCount < 3) throw new InvalidOperationException("HID error");
            });

        _hid.SetFeatureAura(new byte[] { 0x01 }, true);

        Assert.True(callCount >= 3);
    }

    [Fact]
    public void SetFeatureAura_WithoutRetry_DoesNotRetry()
    {
        RegisterDevice(0x1a30);
        int callCount = 0;
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Callback(() =>
            {
                callCount++;
                throw new InvalidOperationException("HID error");
            });

        _hid.RetryCount = 3;
        _hid.SetFeatureAura(new byte[] { 0x01 }, false);

        Assert.Equal(1, callCount);
    }

    #endregion

    #region SetFeatureReport Tests

    [Fact]
    public void SetFeatureReport_UsesSpecifiedReportId()
    {
        RegisterDevice(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        byte[] data = { 0x01, 0x02 };
        _hid.SetFeatureReport(0x5C, data);

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d.Length == 3 && d[0] == 0x5C && d[1] == 0x01 && d[2] == 0x02)
        ), Times.AtLeastOnce);
    }

    #endregion

    #region ReadFeature Tests

    [Fact]
    public void ReadFeature_CallsReaderWithCorrectParams()
    {
        byte[] expected = { 0x01, 0x02, 0x03 };
        _mockWriter.Setup(w => w.ReadFeature(0x5D, 3)).Returns(expected);

        var result = _hid.ReadFeature(0x5D, 3);

        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ReadFeature_ReturnsNullOnFailure()
    {
        _mockWriter.Setup(w => w.ReadFeature(It.IsAny<byte>(), It.IsAny<int>()))
            .Returns((byte[]?)null);

        var result = _hid.ReadFeature(0x5D, 10);

        Assert.Null(result);
    }

    #endregion

    #region WriteBatch Tests

    [Fact]
    public void WriteBatch_SendsAllMessages()
    {
        RegisterDevice(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        var messages = new List<byte[]>
        {
            new byte[] { 0x01 },
            new byte[] { 0x02 },
            new byte[] { 0x03 }
        };

        _hid.WriteBatch(messages, "test");

        _mockWriter.Verify(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()), Times.AtLeast(3));
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public void RetryCount_DefaultIs3()
    {
        Assert.Equal(3, _hid.RetryCount);
    }

    [Fact]
    public void RetryDelayMs_DefaultIs50()
    {
        Assert.Equal(50, _hid.RetryDelayMs);
    }

    [Fact]
    public void RetryDelayMs_CanBeConfigured()
    {
        _hid.RetryDelayMs = 10;
        Assert.Equal(10, _hid.RetryDelayMs);
    }

    [Fact]
    public void RetryCount_CanBeConfigured()
    {
        _hid.RetryCount = 5;
        Assert.Equal(5, _hid.RetryCount);
    }

    [Fact]
    public void ExecuteWithRetry_SucceedsOnLastAttempt()
    {
        RegisterDevice(0x1a30);
        int callCount = 0;
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Callback(() =>
            {
                callCount++;
                if (callCount < 3) throw new InvalidOperationException("fail");
            });

        _hid.RetryCount = 3;
        _hid.RetryDelayMs = 1;
        _hid.SetFeatureAura(new byte[] { 0x01 }, true);

        Assert.True(callCount >= 3);
    }

    #endregion

    #region InitInput Tests

    [Fact]
    public void InitInput_SendsHandshake()
    {
        RegisterDevice(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _hid.InitInput();

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d.Length == 2 && d[0] == AsusHid.INPUT_ID && d[1] == 0x00)
        ), Times.AtLeastOnce);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithWriter_DoesNotThrow()
    {
        var writer = new Mock<IHidReportWriter>();
        Assert.NotNull(new AsusHid(writer.Object));
    }

    [Fact]
    public void Constructor_WithNullWriter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AsusHid(null!));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_Idempotent()
    {
        _hid.Dispose();
        _hid.Dispose();
        Assert.True(true);
    }

    #endregion

    #region Static Write Message Format Tests

    [Fact]
    public void WriteInput_MessageFormat_CorrectLength()
    {
        RegisterDevice(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _hid.WriteInput(new byte[] { 0x01, 0x02 });

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d.Length == 3)
        ), Times.AtLeastOnce);
    }

    [Fact]
    public void Write_MessageFormat_CorrectLength()
    {
        RegisterDevice(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _hid.Write(new byte[] { 0xB3, 0x00 }, "test");

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d.Length == 3 && d[0] == 0x5D)
        ), Times.AtLeastOnce);
    }

    [Fact]
    public void WriteXgm_MessageFormat_CorrectLength()
    {
        RegisterDevice(0x1970);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _hid.WriteXgm(new byte[] { 0x01 }, "test");

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d.Length == 2 && d[0] == 0x5E)
        ), Times.AtLeastOnce);
    }

    #endregion

    #region SetFeatureReport_EdgeCases

    [Fact]
    public void SetFeatureReport_EmptyData_StillPrependsReportId()
    {
        RegisterDevice(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _hid.SetFeatureReport(0x5D, Array.Empty<byte>());

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d.Length == 1 && d[0] == 0x5D)
        ), Times.AtLeastOnce);
    }

    [Fact]
    public void WriteInput_AllZeros_ValidFormat()
    {
        RegisterDevice(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _hid.WriteInput(new byte[] { 0x00, 0x00, 0x00 });

        _mockWriter.Verify(w => w.WriteReport(
            It.IsAny<IHidDeviceStream>(),
            It.Is<byte[]>(d => d[0] == 0x5A && d[1] == 0x00 && d[2] == 0x00 && d[3] == 0x00)
        ), Times.AtLeastOnce);
    }

    #endregion

    #region RegisterStream Tests

    [Fact]
    public void RegisterStream_AddsDevice()
    {
        _hid.RegisterStream(0x1a30);

        Assert.NotNull(_hid.GetDeviceStream(0x1a30));
    }

    [Fact]
    public void GetDeviceStream_ReturnsNullForUnknownPid()
    {
        Assert.Null(_hid.GetDeviceStream(0xFFFF));
    }

    [Fact]
    public void DeviceCount_ReflectsRegisteredStreams()
    {
        _hid.RegisterStream(0x1a30);
        _hid.RegisterStream(0x1854);

        Assert.Equal(2, _hid.DeviceCount);
    }

    #endregion

    #region ProbeAura Tests

    [Fact]
    public void ProbeAura_NoDevices_IsNotAvailable()
    {
        var result = _hid.ProbeAura();

        Assert.False(result.IsAvailable);
        Assert.Equal(AuraLayoutType.FourZone, result.LayoutType);
    }

    [Fact]
    public void ProbeAura_WithPerKeyDevice_DetectsPerKey()
    {
        _hid.RegisterStream(0x1a30);

        var result = _hid.ProbeAura();

        Assert.True(result.IsAvailable);
        Assert.Equal(AuraLayoutType.PerKey, result.LayoutType);
        Assert.Equal(BacklightType.PerKeyAddressable, result.BacklightType);
    }

    [Fact]
    public void ProbeAura_WithRegularDevice_DetectsFourZone()
    {
        _hid.RegisterStream(0x1869);

        var result = _hid.ProbeAura();

        Assert.True(result.IsAvailable);
        Assert.Equal(AuraLayoutType.FourZone, result.LayoutType);
    }

    [Fact]
    public void ProbeAura_KeyboardAlwaysInSupportedZones()
    {
        var result = _hid.ProbeAura();

        Assert.Contains(AuraZone.Keyboard, result.SupportedZones);
    }

    #endregion
}