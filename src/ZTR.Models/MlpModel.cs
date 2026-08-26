namespace ZTR.Models;

public class MlpConfig
{
    public bool Enabled { get; set; } = true;
    public int InputSize { get; set; } = 16;
    public int HiddenLayerSize { get; set; } = 64;
    public int OutputSize { get; set; } = 8;
    public double LearningRate { get; set; } = 0.001;
    public int LearningIntervalSeconds { get; set; } = 30;
    public int PredictionWindowMs { get; set; } = 500;
    public bool AutoModeSwitch { get; set; } = true;
    public bool AutoAffinity { get; set; } = true;
}

public class MlpDecision
{
    public DateTime Timestamp { get; set; }
    public double[] InputFeatures { get; set; } = Array.Empty<double>();
    public double[] OutputActions { get; set; } = Array.Empty<double>();
    public string ActionType { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

public class MlpTrainingSample
{
    public double[] Features { get; set; } = Array.Empty<double>();
    public double[] Target { get; set; } = Array.Empty<double>();
    public DateTime Timestamp { get; set; }
}