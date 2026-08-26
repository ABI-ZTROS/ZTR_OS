using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class WmiHelperTests
{
    private Mock<IWmiQueryService> _mockQueryService;
    private Mock<ILogger<WmiHelper>> _mockLogger;
    private WmiHelper _helper;

    public WmiHelperTests()
    {
        _mockQueryService = new Mock<IWmiQueryService>();
        _mockLogger = new Mock<ILogger<WmiHelper>>();
        _helper = new WmiHelper(_mockQueryService.Object, _mockLogger.Object);
    }

    private void ResetMocks()
    {
        _mockQueryService.Reset();
        _mockLogger.Reset();
        _helper = new WmiHelper(_mockQueryService.Object, _mockLogger.Object);
    }

    private static IDictionary<string, object> CreateDict(params (string key, object value)[] pairs)
    {
        var dict = new Dictionary<string, object>();
        foreach (var (key, value) in pairs)
            dict[key] = value;
        return dict;
    }

    #region GetHardwareModel Tests

    [Fact]
    public void GetHardwareModel_ReturnsManufacturerAndModel()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(new[] { CreateDict(
                ("Manufacturer", "ASUS"),
                ("Model", "ROG Strix G16")) });

        string result = _helper.GetHardwareModel();

        Assert.Equal("ASUS ROG Strix G16", result);
    }

    [Fact]
    public void GetHardwareModel_ReturnsModelOnly_WhenManufacturerMissing()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(new[] { CreateDict(
                ("Manufacturer", ""),
                ("Model", "ROG Strix G16")) });

        string result = _helper.GetHardwareModel();

        Assert.Equal("ROG Strix G16", result);
    }

    [Fact]
    public void GetHardwareModel_ReturnsEmpty_WhenNoResults()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(Array.Empty<IDictionary<string, object>>());

        string result = _helper.GetHardwareModel();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetHardwareModel_ReturnsEmpty_OnException()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        string result = _helper.GetHardwareModel();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetHardwareModel_QueriesCorrectWmiClass()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(Array.Empty<IDictionary<string, object>>());

        _helper.GetHardwareModel();

        _mockQueryService.Verify(q => q.ExecuteQuery(It.Is<string>(s => s.Contains("Win32_ComputerSystem"))), Times.Once);
    }

    #endregion

    #region GetBiosVersion Tests

    [Fact]
    public void GetBiosVersion_ReturnsVersion()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(new[] { CreateDict(("SMBIOSBIOSVersion", "GL503PI.315")) });

        string result = _helper.GetBiosVersion();

        Assert.Equal("GL503PI.315", result);
    }

    [Fact]
    public void GetBiosVersion_ReturnsEmpty_WhenNoResults()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(Array.Empty<IDictionary<string, object>>());

        string result = _helper.GetBiosVersion();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetBiosVersion_ReturnsEmpty_OnException()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        string result = _helper.GetBiosVersion();

        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region GetCpuInfo Tests

    [Fact]
    public void GetCpuInfo_ReturnsName()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(new[] { CreateDict(("Name", "AMD Ryzen 9 7945HX")) });

        string result = _helper.GetCpuInfo();

        Assert.Equal("AMD Ryzen 9 7945HX", result);
    }

    [Fact]
    public void GetCpuInfo_TrimsWhitespace()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(new[] { CreateDict(("Name", "  Intel Core i9-13900K  ")) });

        string result = _helper.GetCpuInfo();

        Assert.Equal("Intel Core i9-13900K", result);
    }

    [Fact]
    public void GetCpuInfo_ReturnsEmpty_OnException()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        string result = _helper.GetCpuInfo();

        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region GetGpuInfo Tests

    [Fact]
    public void GetGpuInfo_ReturnsName()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(new[] { CreateDict(("Name", "NVIDIA GeForce RTX 4090")) });

        string result = _helper.GetGpuInfo();

        Assert.Equal("NVIDIA GeForce RTX 4090", result);
    }

    [Fact]
    public void GetGpuInfo_ReturnsEmpty_OnException()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        string result = _helper.GetGpuInfo();

        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region GetManufacturer Tests

    [Fact]
    public void GetManufacturer_ReturnsName()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(new[] { CreateDict(("Manufacturer", "ASUSTeK COMPUTER INC.")) });

        string result = _helper.GetManufacturer();

        Assert.Equal("ASUSTeK COMPUTER INC.", result);
    }

    [Fact]
    public void GetManufacturer_ReturnsEmpty_OnException()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        string result = _helper.GetManufacturer();

        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region GetDeviceType Tests

    [Theory]
    [InlineData("3", DeviceType.Desktop)]
    [InlineData("4", DeviceType.Desktop)]
    [InlineData("5", DeviceType.Desktop)]
    [InlineData("6", DeviceType.Desktop)]
    [InlineData("7", DeviceType.Desktop)]
    [InlineData("8", DeviceType.Laptop)]
    [InlineData("9", DeviceType.Laptop)]
    [InlineData("10", DeviceType.Laptop)]
    [InlineData("11", DeviceType.Laptop)]
    [InlineData("14", DeviceType.Laptop)]
    [InlineData("12", DeviceType.Tablet)]
    [InlineData("18", DeviceType.Tablet)]
    [InlineData("21", DeviceType.Tablet)]
    [InlineData("1", DeviceType.Unknown)]
    [InlineData("99", DeviceType.Unknown)]
    public void GetDeviceType_MapsChassisTypeCorrectly(string chassisType, DeviceType expected)
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(new[] { CreateDict(("ChassisTypes", new[] { chassisType })) });

        DeviceType result = _helper.GetDeviceType();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetDeviceType_ReturnsUnknown_OnException()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        DeviceType result = _helper.GetDeviceType();

        Assert.Equal(DeviceType.Unknown, result);
    }

    [Fact]
    public void GetDeviceType_ReturnsUnknown_WhenNoResults()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(Array.Empty<IDictionary<string, object>>());

        DeviceType result = _helper.GetDeviceType();

        Assert.Equal(DeviceType.Unknown, result);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void GetHardwareModel_LogsWarning_OnException()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        _helper.GetHardwareModel();

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetBiosVersion_LogsWarning_OnException()
    {
        ResetMocks();
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        _helper.GetBiosVersion();

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}