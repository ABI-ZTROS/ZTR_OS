namespace ZTR.Desktop;

public class BootStats
{
    public int Ok { get; set; }
    public int Fail { get; set; }
    public int Total => Ok + Fail;
}
